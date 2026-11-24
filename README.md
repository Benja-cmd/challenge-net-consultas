# Consultas de Afiliados

Sistema de gestión de consultas médicas para los afiliados de un plan de salud. Permite registrar afiliados, asignar consultas con profesionales de distintas especialidades y hacer seguimiento del estado de cada consulta.

Incluye la política de **cupo de consultas de nutrición**: 6 consultas presenciales por año calendario, no más de 1 por mes, sin acumulación.

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

Los tests (xUnit) están en `tests/ConsultasPlan.Tests` y cubren las reglas de negocio: cupo de nutrición y transiciones de estado de las consultas.

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

**Cupo de nutrición** (aplica a consultas presenciales con profesionales de Nutrición)

- Máximo 6 por año calendario y 1 por mes calendario, según la fecha de la consulta.
- Consumen cupo las consultas en cualquier estado excepto Cancelado.
- Lo no utilizado no se acumula para meses ni años siguientes.
- Las consultas virtuales no consumen ni están limitadas.

## Endpoints

| Método | Ruta | Descripción |
|---|---|---|
| GET | `/consultas` | Lista de consultas |
| GET | `/consultas/{id}` | Detalle de una consulta |
| POST | `/consultas` | Agenda una consulta (valida horario, afiliado, cupo de nutrición) |
| POST | `/consultas/{id}/cancelacion` | Cancela una consulta |
| POST | `/consultas/{id}/atencion` | Registra la atención |
| POST | `/consultas/{id}/inasistencia` | Registra la inasistencia |
| PUT | `/consultas/{id}/estado` | Cambia el estado aplicando las mismas reglas |
| GET | `/afiliados` | Lista de afiliados activos |
| GET | `/afiliados/{id}` | Detalle de un afiliado |
| GET | `/afiliados/{id}/cupo-nutricion?anio=` | Cupo de nutrición usado y disponible |
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
│   ├── Reglas/        # Reglas de negocio puras (estados, cupo de nutrición)
│   └── Servicios/     # Reloj del sistema, cupo de nutrición
├── frontend/    # SPA (Vue 3 · Vite)
└── tests/       # Tests xUnit de las reglas de negocio
```
