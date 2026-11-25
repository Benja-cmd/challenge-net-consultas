# Challenge Técnico — Desarrollador .NET

- **Repositorio:** https://github.com/Benja-cmd/challenge-net-consultas
- **Rama con la solución:** `dev-challenge-qa` (el código original recibido está en `main`)

---

## 1. Resumen

El trabajo se dividió en las dos partes pedidas:

- **Parte 1 — Intervención del código existente.** Se identificaron y corrigieron 32 problemas, desde errores que impedían compilar hasta fallas de reglas de negocio, de integridad de datos y de experiencia de usuario. Los más graves:
  - La solución **no compilaba**.
  - Se podía **crear un afiliado nuevo dentro del alta de una consulta** (over-posting), salteando el control de afiliado bloqueado.
  - `PUT /consultas/{id}/estado` permitía **cualquier cambio de estado** sin ninguna validación.
  - Borrar un afiliado dejaba sus consultas **huérfanas**, y borrar una sede **eliminaba en cascada la historia clínica**.
  - La base se creaba con `EnsureCreated`, que **nunca aplica cambios de esquema** sobre una base en producción.
  - Dos altas simultáneas podían **superar el cupo o tomar el mismo turno** (reproducido: 6 a 9 consultas creadas donde debía haber 1).
- **Parte 2 — Política de cupo.** Modelada como **configuración del plan** (`PoliticaCupo`: plan, especialidad, modalidad, límites), no como constante: la política del enunciado se carga por migración para el "Plan Integral". Regla de dominio pura y testeada, aplicada en el alta con control de concurrencia, expuesta en un endpoint y visible en el frontend.

Se agregaron **54 tests xUnit**: 32 unitarios de las reglas de negocio y 22 de integración que levantan la API completa (incluidos tests de concurrencia).

## 2. Metodología y organización del repositorio

- El primer commit (`main`) contiene el **código recibido sin modificaciones**, para que todo cambio sea rastreable contra el original.
- Siguiendo la convención de branching: rama `dev-challenge-qa` desde `main`; una rama por tema (`bugfix/...`, `feature/...`, `docs/...`) creada desde `dev-challenge-qa` e integrada con merge `--no-ff` (sin Pull Request, según la convención).
- **Commits atómicos**: cada commit corrige un único problema y su mensaje explica *qué* estaba mal, *cómo se reprodujo* y *por qué* se resolvió así. `git log --first-parent dev-challenge-qa` muestra un merge por tema.
- Cada rama se verificó antes de integrarse: compilación, tests, pruebas manuales contra la API (casos válidos y rechazos), base de datos recreada desde cero y sobre una copia de base preexistente y, para el frontend, pruebas en el navegador. Los tests nuevos se validaron provocando el error que deben detectar (p. ej. desactivando la validación de cupo).

| Rama | Commits | Tema |
|---|---|---|
| `bugfix/errores-compilacion` | 2 | La solución no compilaba |
| `bugfix/cancelacion-consultas` | 3 | Reglas y verbo HTTP de la cancelación |
| `bugfix/transiciones-estado-consultas` | 4 | Máquina de estados de la consulta |
| `bugfix/alta-consultas` | 4 | Over-posting y validaciones del alta |
| `bugfix/afiliados` | 4 | Over-posting, validaciones y baja lógica |
| `bugfix/manejo-fechas` | 2 | Mezcla de hora local y UTC |
| `bugfix/base-de-datos` | 3 | Migraciones, índice único, borrados en cascada |
| `bugfix/frontend` | 9 | Errores, refresco de vistas, formato de hora, etc. |
| `feature/cupo-nutricion` | 5 | Parte 2 + proyecto de tests unitarios |
| `docs/actualizar-readme` | 3 | README y este documento |
| `test/integracion-api` | 2 | Tests de punta a punta con `WebApplicationFactory` |
| `feature/cupo-por-plan` | 4 | El cupo pasa a ser configuración del plan |
| `bugfix/concurrencia-cupo` | 1 | Altas simultáneas que superaban cupo y turnos |
| `docs/actualizar-documentacion` | 2 | Actualización de README y documento |

## 3. Parte 1 — Problemas encontrados y cambios realizados

### 3.1 Compilación

| Problema | Cambio | Por qué |
|---|---|---|
| `AfiliadosController.Update` usaba `afiliado.Nombre`, propiedad inexistente (CS1061). | Se usa `NombreCompleto`. | La solución no compilaba. |
| `ConsultasController` usaba `IsInCurrentYear()` sin importar `ConsultasPlan.Helpers` (CS1061). | Se agregó el `using` (luego el helper se reemplazó, ver 3.3). | La solución no compilaba. |

### 3.2 Cancelación de consultas

| Problema | Cambio | Por qué |
|---|---|---|
| Se validaban **23** horas de anticipación; el mensaje y la regla dicen 24. | Umbral de 24 hs. | Permitía cancelar con 23–24 hs. |
| Se podía cancelar una consulta Atendida, con Inasistencia o ya Cancelada. | Solo se cancelan consultas Pendientes o Confirmadas. | Alteraba el historial. |
| La cancelación era `GET /consultas/cancelar/{id}`. | `POST /consultas/{id}/cancelacion` (frontend actualizado). | Un GET debe ser seguro: prefetch del navegador, crawlers o cachés podían cancelar consultas. |

### 3.3 Transiciones de estado

| Problema | Cambio | Por qué |
|---|---|---|
| Reglas dispersas en cada endpoint y dependientes de `DateTime.Now`. | Refactor a `Reglas/TransicionesEstadoConsulta` (clase pura que recibe la hora actual) y un único `CambiarEstado` en el controller. | Una sola fuente de verdad y testeable. |
| Se podía marcar Inasistencia sobre consultas Atendidas o Canceladas. | Solo sobre consultas abiertas (Pendiente/Confirmado). | Pisaba el historial real. |
| Se podía registrar atención de una consulta **futura** o ya cerrada. | Solo consultas abiertas y ya ocurridas. | Registraba atenciones que no existieron. |
| `PUT /consultas/{id}/estado` asignaba cualquier estado sin validar (incluso volver a Pendiente o valores fuera del enum). | Delega en las mismas reglas; Confirmado solo desde Pendiente y con fecha futura; nunca se vuelve a Pendiente; valores inválidos rechazados. | Era un bypass de todas las reglas anteriores. |

### 3.4 Alta de consultas

| Problema | Cambio | Por qué |
|---|---|---|
| **Over-posting**: el endpoint bindeaba la entidad `Consulta` completa. Enviando un `afiliado` anidado, EF **insertaba un afiliado nuevo** y le asignaba la consulta (reproducido: `afiliadoId` 1 terminó como 9), salteando el control de afiliado bloqueado. | DTO `CrearConsultaRequest` con solo los campos editables; la entidad se arma en el servidor. | Vulnerabilidad de integridad y de reglas de negocio. |
| Un POST vacío llegaba a la base; modalidad fuera del enum y motivo vacío aceptados. | DataAnnotations en el DTO (400 con detalle por campo). | Datos inválidos en la base. |
| Se podía agendar en una fecha pasada (p. ej. 2020). | Se rechaza. | Permitía luego "atender" consultas inexistentes. |
| Solo se validaba superposición para el profesional. | También para el afiliado. | Un afiliado podía tener dos turnos a la misma hora. |
| **Condición de carrera**: las validaciones de horario (y luego de cupo) leen y después insertan. Con 20 altas simultáneas se crearon entre 6 y 9 consultas para el mismo turno o el mismo mes de cupo. | Validación + inserción dentro de una transacción `Serializable` (en SQLite, `BEGIN IMMEDIATE`: las altas concurrentes esperan su turno). Tests de concurrencia que antes fallaban y ahora pasan. | Sobreturnos y cupos excedidos en producción con carga real. |

### 3.5 Afiliados

| Problema | Cambio | Por qué |
|---|---|---|
| Over-posting en alta/edición: se podía enviar `bloqueado`, `consultasUsadas`, `plan` o `id`. | DTO `AfiliadoRequest` con datos de contacto. | Un cliente podía auto-desbloquearse o alterar datos administrativos. |
| Sin validaciones; DNI duplicado permitido. | DNI 7–8 dígitos, email válido, teléfono numérico; DNI duplicado → 409. | Calidad e integridad de datos. |
| `DELETE` borraba físicamente y la FK `SET NULL` dejaba las consultas sin afiliado. | **Baja lógica** con el campo `isActive` (existía sin uso). El listado muestra activos; no se editan ni se agendan consultas a inactivos; el detalle sigue disponible para el historial. | Se perdía historia clínica y el registro de consumo del plan. |

### 3.6 Manejo de fechas

| Problema | Cambio | Por qué |
|---|---|---|
| Se mezclaban `DateTime.Now` (validaciones, seed) y `DateTime.UtcNow` (`FechaCreacion`, `createdAt`). Reproducido: dos registros creados en el mismo momento con 3 hs de diferencia. Además el comportamiento dependía de la zona del servidor. | Servicio `IReloj` (basado en `TimeProvider`) con zona horaria configurable (`America/Argentina/Buenos_Aires`). Todas las fechas salen de ahí; fechas recibidas con zona explícita se convierten. | Consistencia de datos, independencia del servidor y testeabilidad. |

### 3.7 Base de datos

| Problema | Cambio | Por qué |
|---|---|---|
| `EnsureCreated` crea el esquema una vez y nunca lo actualiza. | Migraciones de EF Core. La migración inicial replica exactamente el esquema anterior; `MigradorBaseDatos` detecta bases creadas con `EnsureCreated`, las marca como migradas y aplica solo lo nuevo. | Ningún cambio de modelo llegaba a una base existente. Probado con una copia de base previa: sin pérdida de datos. |
| Sin restricción de unicidad para el DNI. | Índice único `IX_Afiliados_DNI`. | La validación en la API no cubre concurrencia ni escrituras externas. |
| Cadena de conexión fija en `Program.cs`. | Se toma de `ConnectionStrings:Default`. | Permite otra base por entorno y aislar los tests. |
| FKs `Sede→Profesional` y `Profesional→Consulta` en CASCADE; `Afiliado→Consulta` en SET NULL. Borrar una sede eliminaba todas sus consultas. | Todas en RESTRICT. | Ninguna baja debe arrastrar historia clínica. Verificado: la base rechaza esos DELETE. |

### 3.8 Frontend

| Problema | Cambio | Por qué |
|---|---|---|
| Todos los errores se mostraban como "Error al procesar la solicitud". | `mensajeDeError()` muestra el mensaje real del backend (reglas y validaciones). | El usuario no sabía por qué fallaba. |
| Cancelar / marcar inasistencia no refrescaban la vista ni capturaban errores; tras registrar atención el detalle perdía afiliado y profesional. | El detalle recarga la consulta completa tras cada acción; el listado actualiza la fila. | La pantalla mostraba información incorrecta. |
| Se ofrecían siempre todas las acciones y un selector con los 5 estados. | Solo se muestran las acciones válidas según estado y fecha. | Inducía a operaciones que el backend rechaza. |
| Baja de afiliado con un clic, sin confirmación. | `confirm()` con nombre y DNI; botón "Dar de baja". | Prevención de errores operativos. |
| URL de la API fija en el código. | Variable `VITE_API_URL` + `.env.example`. | Desplegar en otro entorno requería modificar el código. |
| Formulario de nueva consulta enviable vacío, con fecha pasada o doble clic. | `required`, fecha mínima, `maxlength`, afiliados bloqueados deshabilitados, botón deshabilitado al guardar. | Errores evitables y solicitudes duplicadas. |
| **Encontrado al probar en el navegador:** `toLocaleString('es-AR')` usa ciclo de 12 hs sin a. m./p. m.: una consulta de las 15:00 se mostraba como "03:00". | Formateador común con `hourCycle: 'h23'`. | Información horaria incorrecta. |
| Al navegar de `/consultas/17` a `/consultas/2` se seguía mostrando la 17. | Se observa el parámetro de la ruta. | Vue Router reutiliza el componente. |
| Sin `package-lock.json`. | Se versiona. | Builds reproducibles. |

## 4. Parte 2 — Política de cupo de consultas

> "Implementar política de cupo de consultas: el plan incluye 6 consultas de nutrición por año, no más de 1 por mes. Las consultas no usadas no se acumulan. Aplica a consultas presenciales."

### Diseño

El enunciado habla de una *política* que *el plan incluye*, y cada afiliado tiene un `Plan`. Por eso el cupo se modeló como configuración del plan y no como una constante en el código:

- **`PoliticaCupo`** (tabla `PoliticasCupo`) — plan, especialidad, modalidad, límite anual, límite mensual y si la inasistencia consume cupo. Índice único por plan + especialidad + modalidad. La política del enunciado (*Plan Integral · Nutrición · Presencial · 6 por año · 1 por mes*) se inserta en la **migración**, para que llegue también a bases existentes. Agregar otra política (otro plan u otra especialidad) no requiere cambiar código.
- **`Reglas/ReglasCupo`** — regla de dominio pura (sin base de datos ni hora actual): si una política aplica a un afiliado/profesional/modalidad, si una consulta existente consume cupo y si una nueva fecha excede el límite anual o mensual.
- **`Servicios/CupoServicio`** — resuelve la política que corresponde al plan del afiliado y obtiene de la base las consultas que la consumen.
- **`POST /consultas`** — si hay una política aplicable, valida el cupo dentro de la misma transacción serializable que el resto del alta; si se excede responde 400 con un mensaje explicativo (p. ej. *"El afiliado ya utilizó el cupo de consultas presenciales de nutrición de noviembre de 2026. Su plan permite 1 por mes."*).
- **`GET /afiliados/{id}/cupo?profesionalId=&modalidad=&anio=`** — estado del cupo aplicable (usadas, disponibles, meses utilizados) o `204` si esa combinación no tiene cupo.
- **Frontend** — al elegir afiliado, profesional y modalidad, el formulario consulta ese endpoint y, si hay cupo, lo muestra y advierte si está agotado o si el mes ya está usado. No duplica la regla: qué está alcanzado lo decide el backend.

### Verificación

- **Unitarios** (`tests/.../Reglas`): límites anual y mensual, límites configurables, no acumulación entre meses y años, cambio de año, plan/especialidad/modalidad que no aplican, estados que consumen cupo e inasistencia configurable.
- **Integración** (`tests/.../Integracion`): la API completa con una base temporal y fecha fija (24/11/2026) sobre los datos del seed, cada afiliado pensado para un escenario: 6 usadas (rechaza presencial, permite virtual y enero del año siguiente), 5 usadas (permite la 6.ª, rechaza la 7.ª), mes ya ocupado, virtual y cancelada que no cuentan, cancelación que libera cupo, otra especialidad, afiliado de un plan sin política, límites tomados de la configuración, endpoint de cupo y concurrencia.
- Migración aplicada sobre una copia de una base creada con el código original: la política se inserta y los datos se conservan.

## 5. Supuestos ante puntos ambiguos

| Punto ambiguo | Supuesto adoptado | Motivo |
|---|---|---|
| A quién aplica | A los afiliados cuyo plan tiene una política configurada; hoy, "Plan Integral" (el de todos los afiliados del seed). | El enunciado dice "el plan incluye". Un afiliado de un plan sin política no tiene límite. |
| "Por año" | Año **calendario** (enero–diciembre). | Alternativa considerada: año desde la fecha de alta. El seed tiene altas cerca del cambio de año (14/12, 23/12, 30/12, 04/01), donde ambas lecturas difieren por días. Se eligió calendario porque es como se comunican los topes anuales de prestaciones, es coherente con el límite por mes calendario y no exige que el afiliado conozca su fecha de alta. Si se definiera lo contrario, el cambio queda acotado al cálculo del período en `ReglasCupo`. |
| "Por mes" | Mes **calendario**. | Coherente con el criterio anual. |
| Qué fecha cuenta | La fecha **de la consulta** (`FechaHora`), no la de creación. | El cupo limita prestaciones, no reservas. Una consulta de enero agendada en diciembre consume cupo del año siguiente. |
| Qué estados consumen cupo | Todos salvo **Cancelado**. La **inasistencia** consume cupo por defecto, configurable por política (`InasistenciaConsumeCupo`). | Una consulta pendiente reserva el turno y la inasistencia ocupó el turno del profesional sin aviso (cancelar con 24 hs sí libera cupo). Es el punto más discutible: en el seed, el campo `ConsultasUsadas` de Laura vale 0 aunque tiene una inasistencia, lo que podría sugerir lo contrario; pero ese campo no es confiable (Diego figura con 1 sin consultas presenciales válidas y Carolina con 1 por una consulta del año anterior). Por eso se dejó configurable. |
| "No se acumulan" | El cupo se calcula siempre sobre el mes y año de la consulta: lo no usado no habilita más consultas en otro mes ni pasa al año siguiente. | Interpretación literal. |
| "Aplica a consultas presenciales" | Las virtuales de nutrición **no consumen ni están limitadas**. | Interpretación literal. |
| Qué es "nutrición" | Profesional cuya especialidad coincide con la de la política, sin distinguir mayúsculas, tildes ni espacios. | La especialidad es texto libre en el modelo. |
| Campo `Afiliado.ConsultasUsadas` | No se usa para el cupo: se calcula desde las consultas. | Ningún proceso lo actualizaba y sus valores del seed no coinciden con las consultas (ver arriba); derivar el dato evita inconsistencias. No se eliminó para no cambiar el contrato de la API. |
| Zona horaria | Todo el sistema opera en hora de Argentina; fechas sin zona se interpretan como hora local. | Es lo que envía el `datetime-local` del frontend y lo esperable para turnos presenciales. |
| Superposición de turnos | Coincidencia exacta de fecha y hora (criterio original). | El modelo no tiene duración de consulta. |
| Transiciones de estado | Cancelado, Atendido e Inasistencia son finales; solo se confirma una consulta pendiente y futura. | No hay especificación; se eligió la regla más conservadora con el historial. |
| Motivo de la consulta | Obligatorio (máx. 500). | La columna es `NOT NULL` en la base. |
| Unicidad del DNI | Única entre todos los afiliados, activos e inactivos. | Reingresar a alguien implica reactivarlo, no duplicarlo. |

## 6. Observaciones sobre el código original que se mantuvieron

| Observación | Decisión |
|---|---|
| "Solo se pueden registrar atenciones de consultas del año en curso": una consulta del 31/12 no puede registrarse como atendida el 2/1. | Se mantuvo porque es una regla existente y explícita (probablemente ligada a cierres anuales), pero es discutible y la consultaría con negocio. |
| `Afiliado.FechaBloqueo` sugiere que el bloqueo podría vencer o revisarse, pero no hay ninguna regla que lo use (Facundo está bloqueado desde abril). | Se respetó el bloqueo como permanente; no hay especificación para inferir un vencimiento. |
| Propiedades con nombres inconsistentes (`createdAt`, `isActive`). | No se renombraron: cambiaría el contrato JSON y el esquema sin aportar a los requerimientos. |

## 7. Fuera de alcance

| Tema | Motivo / cómo lo abordaría |
|---|---|
| Datos históricos en UTC: registros creados antes del fix de fechas tienen `FechaCreacion`/`createdAt` en UTC. | No se pueden distinguir con certeza de los que están en hora local; se documenta en lugar de migrarlos. |
| `Consulta.AfiliadoId` sigue siendo nullable. | Una base en producción puede tener consultas huérfanas generadas por el bug anterior; forzar `NOT NULL` haría fallar la migración. Requiere primero una depuración de datos. |
| Cancelar automáticamente las consultas futuras al dar de baja un afiliado. | Es una decisión de negocio no especificada. |
| Reactivación de afiliados. | No pedida. |
| Autenticación/autorización y CORS abierto (`AllowAnyOrigin`), Swagger expuesto siempre. | Afectan a todo el sistema y requieren definir el modelo de usuarios y el entorno de despliegue. |
| Respuestas de la API con entidades EF (incluye ciclos y datos del afiliado en cada consulta). | Reemplazarlas por DTOs de salida cambia el contrato del frontend; se priorizaron los DTOs de entrada, que eran el problema de seguridad. |
| Especialidad como texto libre. | Lo correcto sería una entidad/catálogo de especialidades; se mitigó comparando de forma normalizada. |
| Vulnerabilidades de `vite`/`esbuild` reportadas por `npm audit`. | Afectan solo al servidor de desarrollo y la corrección requiere vite 8 (cambio mayor). |
| Administración de políticas de cupo (ABM por API/pantalla). | Hoy se configuran por migración o en la base; el modelo ya lo soporta. |
| Tests automatizados del frontend. | El frontend se verificó manualmente en el navegador; la lógica de negocio está en el backend y cubierta por tests. |
| Concurrencia en cambios de estado (p. ej. cancelar y atender a la vez). | Riesgo bajo; se priorizó el alta, que es donde se validan cupo y turnos. |
| Paginación de listados, manejo de errores global (middleware), logging estructurado. | Mejoras transversales sin impacto en los requerimientos. |

## 8. Cómo continuaría con más tiempo

1. Validar con negocio los supuestos marcados como discutibles (inasistencia, año calendario, regla de atención del año en curso).
2. Catálogo de especialidades y entidad `Plan` (hoy texto libre), con ABM de políticas de cupo.
3. DTOs de salida y un middleware de errores con un formato único (`ProblemDetails`).
4. Tests del frontend (Vitest) y un pipeline de CI que corra build y tests en cada push.
5. Autenticación y roles (administrativo, profesional).

## 9. Cómo ejecutar

Ver `README.md`. En resumen:

```bash
cd backend && dotnet run                # API en http://localhost:5000 (Swagger en /swagger)
cd frontend && npm install && npm run dev   # Frontend en http://localhost:5173
dotnet test backend/ConsultasPlan.sln   # 54 tests (unitarios + integración)
```
