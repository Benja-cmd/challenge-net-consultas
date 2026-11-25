# Consultas de Afiliados

Sistema de gestión de consultas médicas para los afiliados de un plan de salud. Permite registrar afiliados, asignar consultas con profesionales de distintas especialidades y hacer seguimiento del estado de cada consulta.

Incluye **políticas de cupo de consultas por plan**. La configurada por defecto: el *Plan Integral* incluye 6 consultas presenciales de nutrición por año calendario, no más de 1 por mes, sin acumulación.

## Requisitos

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [Node.js 18+](https://nodejs.org/)

---

## Backend

```bash
cd backend
dotnet run
```

El servidor levanta en **http://localhost:5000**.

La base de datos SQLite (`consultas.db`) se crea automáticamente en la primera ejecución aplicando las migraciones de EF Core, junto con los datos iniciales. Si ya existe una base creada por una versión anterior (con `EnsureCreated`), se adopta sin perder datos y se le aplican las migraciones pendientes.

**Swagger:** http://localhost:5000/swagger

### Configuración

En `backend/appsettings.json`:

| Clave | Descripción | Valor por defecto |
|---|---|---|
| `ConnectionStrings:Default` | Cadena de conexión SQLite | `Data Source=consultas.db` |
| `ZonaHoraria` | Zona horaria en la que se interpretan y validan todas las fechas del sistema | `America/Argentina/Buenos_Aires` |

### Migraciones

`dotnet-ef` está declarado como herramienta local del proyecto:

```bash
cd backend
dotnet tool restore
dotnet ef migrations add <Nombre> -o Data/Migrations
```

---

## Tests

```bash
dotnet test backend/ConsultasPlan.sln
```

Los tests (xUnit) están en `tests/ConsultasPlan.Tests`:

- `Reglas/`: tests unitarios de las reglas de negocio (cupo y transiciones de estado).
- `Integracion/`: levantan la API completa con `WebApplicationFactory`, una base SQLite temporal por test y una fecha fija, y prueban los escenarios del seed de punta a punta, incluida la concurrencia en altas simultáneas.

---

## Frontend

```bash
cd frontend
npm install
npm run dev
```

El cliente levanta en **http://localhost:5173**.

La URL de la API se configura con la variable `VITE_API_URL` (por defecto `http://localhost:5000`). Para otro entorno, copiar `frontend/.env.example` como `frontend/.env.local` y ajustarla.

---

## Reglas de negocio principales

**Estados de una consulta**

```
Pendiente ─┬→ Confirmado ─┬→ Cancelado     (con 24 hs o más de anticipación)
           └──────────────┼→ Atendido      (consulta ya ocurrida, del año en curso)
                          └→ Inasistencia  (consulta ya ocurrida)
Cancelado, Atendido e Inasistencia son estados finales.
```

**Cupo de consultas por plan** (tabla `PoliticasCupo`)

Cada política define plan, especialidad, modalidad, límite anual, límite mensual y si la inasistencia consume cupo. La migración `AgregarPoliticasDeCupo` carga la del challenge: *Plan Integral · Nutrición · Presencial · 6 por año · 1 por mes*.

- Los períodos son año y mes calendario, según la fecha de la consulta.
- Consumen cupo las consultas en cualquier estado excepto Cancelado (la inasistencia, según la política).
- Lo no utilizado no se acumula para meses ni años siguientes.
- Las combinaciones sin política (otra especialidad, modalidad virtual, otro plan) no tienen límite.
- El alta de consultas se ejecuta en una transacción serializable: altas simultáneas no pueden superar el cupo ni tomar el mismo turno.

## Endpoints

| Método | Ruta | Descripción |
|---|---|---|
| GET | `/consultas` | Lista de consultas |
| GET | `/consultas/{id}` | Detalle de una consulta |
| POST | `/consultas` | Agenda una consulta (valida horario, afiliado y cupo del plan) |
| POST | `/consultas/{id}/cancelacion` | Cancela una consulta |
| POST | `/consultas/{id}/atencion` | Registra la atención |
| POST | `/consultas/{id}/inasistencia` | Registra la inasistencia |
| PUT | `/consultas/{id}/estado` | Cambia el estado aplicando las mismas reglas |
| GET | `/afiliados` | Lista de afiliados activos |
| GET | `/afiliados/{id}` | Detalle de un afiliado |
| GET | `/afiliados/{id}/cupo?profesionalId=&modalidad=&anio=` | Cupo aplicable usado y disponible (204 si no aplica ninguno) |
| POST | `/afiliados` | Alta de afiliado |
| PUT | `/afiliados/{id}` | Edición de datos de contacto |
| DELETE | `/afiliados/{id}` | Baja lógica del afiliado |
| GET | `/profesionales` | Lista de profesionales |
| GET | `/sedes` | Lista de sedes |

---

## Estructura del proyecto

```
/
├── backend/     # API REST (.NET 8 · EF Core · SQLite)
│   ├── Controllers/
│   ├── Data/          # DbContext, seed, migraciones
│   ├── Dtos/          # Contratos de entrada/salida
│   ├── Models/
│   ├── Reglas/        # Reglas de negocio puras (estados, cupo)
│   └── Servicios/     # Reloj del sistema, resolución de cupos por plan
├── frontend/    # SPA (Vue 3 · Vite)
└── tests/       # Tests xUnit unitarios y de integración
```
