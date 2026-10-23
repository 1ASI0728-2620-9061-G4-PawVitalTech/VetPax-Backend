# VetPax Backend API — Documentación técnica

> **Estado actual:** API de `PetClinicalCare` implementada y probada parcialmente. Este README documenta **lo que existe hasta ahora**, no todas las funcionalidades previstas del producto.

## 📌 Descripción general

**VetPax** es una solución para gestionar la atención veterinaria de mascotas. El backend expone servicios REST para registrar mascotas, consultar sus datos e historial clínico y preparar el registro de atenciones y la evaluación de su evolución.

Actualmente se ha desarrollado principalmente el Bounded Context **PetClinicalCare**. La autenticación definitiva, los permisos veterinarios y los demás módulos continúan en desarrollo.

## 🧱 Arquitectura

El backend está organizado como un **monolito modular**, con **Domain-Driven Design (DDD)** y **arquitectura hexagonal**. Existe una sola API ASP.NET Core; las dependencias se dirigen hacia el dominio.

| Proyecto | Responsabilidad |
|---|---|
| `VetPax.Domain` | Agregados, entidades, Value Objects, eventos, fábricas, políticas y servicios de dominio. |
| `VetPax.Application` | Casos de uso (`Commands`, `Queries`, `Handlers`) y puertos. |
| `VetPax.Infrastructure` | EF Core, MySQL, repositorios, Unit of Work y adaptadores externos. |
| `VetPax.Api` | Controllers REST, recursos HTTP, configuración e inyección de dependencias. |
| `VetPax.Tests` | Pruebas unitarias, de integración con MySQL y de aceptación BDD. |

### Bounded Contexts del diseño de VetPax

| Bounded Context | Responsabilidad | Estado |
|---|---|---|
| **PetClinicalCare** | Mascotas, historial, atenciones y evolución clínica. | Implementación principal en curso. |
| **ClinicManagement** | Clínicas, profesionales y pacientes vinculados. | Pendiente. |
| **AppointmentManagement** | Citas y agendas veterinarias. | Pendiente. |
| **IdentityAccessManagement** | Identidad, autenticación y autorización. | Pendiente de integración. |
| **MedicationTreatment** | Medicación y tratamientos. | Pendiente. |
| **NutritionManagement** | Planes de alimentación. | Pendiente. |
| **AdherenceGamification** | Adherencia, progresos y gamificación. | Pendiente. |

## ⚙️ Tecnologías y requisitos

- **.NET 10 SDK** (el proyecto de pruebas actual utiliza `net10.0`).
- **ASP.NET Core Web API** y C#.
- **MySQL 8.x** instalado y en ejecución.
- **Entity Framework Core 9** + proveedor `Pomelo.EntityFrameworkCore.MySql` 9.x.
- **Swagger UI / OpenAPI** para documentar y probar endpoints.
- **Git** y, opcionalmente, **JetBrains Rider**.
- Para pruebas: **xUnit**, **NSubstitute** y **Reqnroll/Gherkin**, según la configuración del proyecto de pruebas.

> Se recomienda usar las mismas versiones de SDK y paquetes registradas en los archivos `.csproj`. No actualices paquetes de EF Core de forma aislada: Pomelo y EF Core deben ser compatibles.

## 🚀 Clonación, configuración y ejecución

Todos los comandos siguientes se ejecutan desde la **raíz del repositorio**, salvo indicación contraria.

### 1. Clonar el repositorio

```bash
git clone <URL_DEL_REPOSITORIO>
cd VetPax-Backend
```

Sustituye `<URL_DEL_REPOSITORIO>` por la URL real de GitHub que comparta el equipo. Si el repositorio tiene otro nombre, usa esa carpeta en el comando `cd`.

### 2. Restaurar dependencias

```bash
dotnet restore
dotnet build
```

### 3. Crear la base de datos MySQL

En MySQL Workbench o en una consola MySQL:

```sql
CREATE DATABASE IF NOT EXISTS vetpax_db
CHARACTER SET utf8mb4
COLLATE utf8mb4_unicode_ci;
```

No crees las tablas a mano. Se generan con las **migraciones incluidas en el repositorio**.

### 4. Configurar la conexión local de forma segura

Las contraseñas **no deben subirse a GitHub ni guardarse en el `appsettings.json` compartido**. Para desarrollo, usar **.NET User Secrets**:

```bash
dotnet user-secrets init --project src/VetPax.Api

dotnet user-secrets set "ConnectionStrings:VetPaxDb" "Server=localhost;Port=3306;Database=vetpax_db;User=root;Password=TU_PASSWORD;" --project src/VetPax.Api
```

Reemplaza `TU_PASSWORD` con tu contraseña de MySQL. Si el proyecto ya tiene configurado User Secrets, no es necesario repetir `init`.

Para uso compartido o despliegue, configura `ConnectionStrings__VetPaxDb` como variable de entorno en el servidor, sin publicar las credenciales.

### 5. Aplicar migraciones

Las migraciones de `PetClinicalCare` ya fueron creadas y se encuentran en:

```text
src/VetPax.Infrastructure/Persistence/Migrations/
```

**Al clonar no se debe ejecutar `migrations add`**: ese comando crea una *nueva* migración y se usa únicamente cuando cambia el modelo persistente.

Para aplicar las migraciones existentes:

```bash
dotnet tool install --global dotnet-ef --version 9.0.0

dotnet ef database update \
  --project src/VetPax.Infrastructure \
  --startup-project src/VetPax.Api
```

> En PowerShell, escribe el comando `dotnet ef database update` en **una sola línea** (los `\` de arriba son continuaciones para shells tipo Bash):
>
> `dotnet ef database update --project src/VetPax.Infrastructure --startup-project src/VetPax.Api`

**Migración automática en Development:** el `Program.cs` actual también utiliza `Database.MigrateAsync()` al arrancar en el entorno Development. Por eso, si la conexión y los permisos son correctos, el backend aplica migraciones pendientes automáticamente. El comando manual anterior es una alternativa de verificación. La base MySQL debe existir y estar accesible.

### 6. Ejecutar la API

```bash
dotnet run --project src/VetPax.Api --launch-profile https
```

También puedes ejecutar **VetPax.Api: https** desde Rider. Asegúrate de ejecutar con el entorno `Development`.

Abre Swagger UI en:

```text
https://localhost:<PUERTO_HTTPS>/swagger
```

Reemplaza `<PUERTO_HTTPS>` por el puerto mostrado en la consola o en `Properties/launchSettings.json`. No se presupone un puerto fijo.

El documento OpenAPI se expone en Development en `/openapi/v1.json`.

## 🔐 Autenticación y permisos: estado actual

**Keycloak/IAM todavía no está integrado**. Por seguridad, los Controllers actualmente permiten los encabezados de identidad de desarrollo **únicamente** cuando la API se ejecuta en `Development`.

| Encabezado | Se utiliza en | Significado |
|---|---|---|
| `X-Dev-Owner-Id` | Registro/consulta de mascotas e historial. | UUID temporal del propietario. |
| `X-Dev-User-Id` | Registro de atención y evolución. | UUID temporal del solicitante veterinario. |
| `X-Dev-Clinic-Id` | Registro de atención y evolución. | UUID temporal de la clínica. |

**Importante:** estos encabezados **no son autenticación**. No habilitar los endpoints en producción basándose en ellos.

El adaptador de permisos veterinarios actual, `DenyAllClinicalAuthorizationAdapter`, deniega el registro de atenciones y la consulta de evolución hasta implementar IAM y la validación de vinculación clínica.

## 📦 Endpoints actuales

Base de las rutas: `/api/v1`.

| Método | Endpoint | Historia | Qué hace | Estado actual |
|---|---|---|---|---|
| `POST` | `/api/v1/pets` | US01 / TS01 | Registra una mascota y abre su historial vacío. | **Operativo en Development** (`201`). |
| `GET` | `/api/v1/pets/{petId}` | TS01 | Consulta los datos básicos de una mascota. | **Operativo en Development** (`200`, `404`, `403`). |
| `GET` | `/api/v1/pets/{petId}/clinical-records` | US02 / TS02 | Consulta historial y atenciones cronológicamente. | **Operativo en Development** (`200` incluso si está vacío; `403`/`404`). |
| `POST` | `/api/v1/pets/{petId}/clinical-records` | US03 / TS02 | Registra una atención con profesional responsable. | **Ruta implementada; acceso veterinario bloqueado** (`403`). |
| `GET` | `/api/v1/pets/{petId}/evolution` | US11 / TS08 | Calcula evolución a partir de indicadores comparables. | **Ruta implementada; acceso veterinario bloqueado** (`403`). |

### 1. Registrar mascota

**`POST /api/v1/pets`**

Encabezado en Development:

```text
X-Dev-Owner-Id: 11111111-1111-4111-8111-111111111111
```

Cuerpo JSON:

```json
{
  "name": "Luna",
  "species": "Dog",
  "birthDate": "2022-05-10",
  "breed": "Golden Retriever",
  "sex": "Female"
}
```

Respuesta exitosa: **`201 Created`**; contiene `petId` y `clinicalRecordId` (UUID generados por el backend). Los nombres y valores de los enums se envían como cadenas (`Dog`, `Cat`, `Male`, `Female`).

### 2. Consultar mascota

**`GET /api/v1/pets/{petId}`** con el mismo `X-Dev-Owner-Id` usado en el registro.

Respuesta `200 OK`: datos básicos de la mascota, incluidos nombre, especie, fecha de nacimiento, raza, sexo y fecha de registro.

- `404 Not Found`: mascota inexistente.
- `403 Forbidden`: propietario distinto al asociado.

### 3. Consultar historial clínico

**`GET /api/v1/pets/{petId}/clinical-records`** con `X-Dev-Owner-Id`.

Ejemplo de respuesta de un historial nuevo (`200 OK`, valores de muestra):

```json
{
  "petId": "11111111-aaaa-4aaa-8aaa-111111111111",
  "clinicalRecordId": "22222222-bbbb-4bbb-8bbb-222222222222",
  "lastUpdatedAt": "2026-10-23T00:00:00Z",
  "message": "No clinical attentions have been recorded yet.",
  "attentions": []
}
```

Cuando existen atenciones, se devuelven **ordenadas cronológicamente**, con sus indicadores. Se rechazan consultas sin autorización (`403`).

### 4. Registrar atención clínica — pendiente de habilitar

**`POST /api/v1/pets/{petId}/clinical-records`**

Encabezados de prueba: `X-Dev-User-Id` y `X-Dev-Clinic-Id` (UUID válidos).

Cuerpo JSON preparado:

```json
{
  "attentionDate": "2026-10-23T14:30:00Z",
  "reason": "Control general",
  "diagnosis": "Paciente estable",
  "treatmentSummary": "Seguimiento preventivo",
  "observations": "Sin novedades",
  "indicators": [
    { "name": "Weight", "value": 10.5, "unit": "kg" }
  ]
}
```

**Estado actual:** aunque el Controller y el caso de uso existen, la autorización devuelve `403 Forbidden`. La respuesta `201 Created` solo será posible una vez integrado el acceso de profesionales autorizados.

### 5. Consultar evolución clínica — pendiente de habilitar

**`GET /api/v1/pets/{petId}/evolution`** con encabezados veterinarios de prueba.

**Estado actual:** devuelve `403 Forbidden` para accesos no autorizados. La lógica de dominio ya prepara la serie de indicadores comparables; el modelo de respuesta incluye `hasSufficientData` y `entries`. Se requiere autorización veterinaria real para habilitar el caso de uso.

## 🧠 Reglas de negocio implementadas

- Cada mascota pertenece a un propietario y recibe un identificador único.
- Al registrar una mascota se crea también un historial clínico vacío; ambos agregados se guardan mediante `UnitOfWork`.
- El propietario puede consultar únicamente sus mascotas y sus historiales en los endpoints de desarrollo actuales.
- Una atención requiere identificadores válidos, fecha, motivo, diagnóstico y resumen de tratamiento.
- Las atenciones del historial se consultan por fecha cronológica e incluyen indicadores.
- La evolución clínica agrupa mediciones por nombre y unidad del indicador; se consideran suficientes dos mediciones comparables en fechas distintas. **Esta es una decisión actual de implementación que debe validarse con el equipo**.
- Los eventos de dominio y los avisos clínicos están modelados, pero los adaptadores de desarrollo **solo escriben logs**, no notifican usuarios ni garantizan entrega.

## 🧪 Pruebas automatizadas

El proyecto `VetPax.Tests` contiene pruebas relacionadas con el capítulo VII del Trabajo Final:

| Tipo | Cantidad actualmente implementada | Alcance |
|---|---:|---|
| **Unitarias** | 6 | Entidades, fábricas, políticas, evolución y Handlers. |
| **Integración con MySQL** | 3 | Persistencia de US01 y consultas de US02. |
| **Aceptación BDD** | 4 escenarios | Gherkin + Steps para registro y consulta del historial (US01 y US02). |
| **Total** | **13** | Pruebas informadas como ejecutadas en el entorno de desarrollo. |

### Preparar la base de pruebas

```sql
CREATE DATABASE IF NOT EXISTS vetpax_test_db
CHARACTER SET utf8mb4
COLLATE utf8mb4_unicode_ci;
```

Las pruebas usan **otra** base de datos para no alterar `vetpax_db`. Configura la variable de entorno `VETPAX_TEST_MYSQL` en el **Test Runner de Rider** o en la terminal desde la que ejecutes los tests:

**PowerShell (Windows):**

```powershell
$env:VETPAX_TEST_MYSQL = "Server=localhost;Port=3306;Database=vetpax_test_db;User=root;Password=TU_PASSWORD;"
dotnet test tests/VetPax.Tests/VetPax.Tests.csproj
```

La variable es de la sesión actual. En Rider, si ya quedó configurada para el ejecutor de pruebas, puedes usar **Run All tests from VetPax.Tests**.

Las pruebas de integración trabajan con migraciones y transacciones de reversión. Las pruebas BDD usan `WebApplicationFactory` y limpian sus datos al finalizar cada escenario.

### Evidencias para el informe

Registrar para cada Sprint: pruebas y comportamientos cubiertos, capturas de los resultados, relación con las US y referencias al repositorio/commits. Los `.feature` y Steps forman parte del código fuente de las pruebas de aceptación.

## 📂 Estructura del repositorio (simplificada)

```text
VetPax-Backend/
├── src/
│   ├── VetPax.Api/
│   │   ├── Modules/PetClinicalCare/
│   │   │   ├── Controllers/
│   │   │   └── Resources/
│   │   ├── DependencyInjection.cs
│   │   └── Program.cs
│   ├── VetPax.Application/
│   │   ├── Common/Ports/
│   │   └── Modules/PetClinicalCare/
│   │       ├── Ports/
│   │       └── UseCases/
│   ├── VetPax.Domain/
│   │   ├── Shared/Events/
│   │   └── Modules/PetClinicalCare/
│   │       ├── Aggregates/
│   │       ├── Entities/
│   │       ├── Events/
│   │       ├── Factories/
│   │       ├── Repositories/
│   │       ├── Services/
│   │       └── ValueObjects/
│   └── VetPax.Infrastructure/
│       ├── Messaging/
│       ├── Modules/PetClinicalCare/
│       │   ├── Adapters/
│       │   └── Repositories/
│       └── Persistence/
│           ├── Migrations/
│           ├── VetPaxDbContext.cs
│           └── UnitOfWork.cs
├── tests/
│   └── VetPax.Tests/
│       ├── Domain/
│       ├── Application/
│       ├── Integration/
│       └── Acceptance/
│           ├── Features/
│           ├── Steps/
│           └── Support/
└── README.md
```

## 🛠️ Comandos útiles

| Tarea | Comando desde la raíz |
|---|---|
| Restaurar paquetes | `dotnet restore` |
| Compilar | `dotnet build` |
| Iniciar API | `dotnet run --project src/VetPax.Api --launch-profile https` |
| Aplicar migraciones existentes | `dotnet ef database update --project src/VetPax.Infrastructure --startup-project src/VetPax.Api` |
| Ejecutar pruebas | `dotnet test tests/VetPax.Tests/VetPax.Tests.csproj` |

Para generar una **nueva** migración, solo después de modificar el mapeo de entidades:

```bash
dotnet ef migrations add NombreDelCambio --project src/VetPax.Infrastructure --startup-project src/VetPax.Api --output-dir Persistence/Migrations
```

Confirma el contenido de cada nueva migración antes de aplicarla y súbela al repositorio junto con el cambio de código.

## 🚧 Pendientes antes de considerar terminado PetClinicalCare

1. Integrar **IdentityAccessManagement / Keycloak** y obtener identidad/roles desde tokens verificados.
2. Comprobar los vínculos veterinario–clínica–paciente mediante **ClinicManagement**, reemplazando el adaptador `DenyAllClinicalAuthorizationAdapter`.
3. Habilitar y probar los casos completos de **US03** (registro clínico) y **US11** (evolución clínica) con permisos reales.
4. Sustituir los adaptadores de logging por publicación fiable de eventos (por ejemplo, Outbox) y notificaciones reales para las funcionalidades de tiempo real.
5. Completar las pruebas y evidencias de los casos autorizados y las integraciones entre Bounded Contexts.
6. Continuar los Bounded Contexts restantes del backend de acuerdo con el capítulo V del informe VetPax.

## 🤝 Convenciones para desarrolladores

- Mantener las reglas de negocio en **Domain**, la coordinación en **Application** y los detalles externos en **Infrastructure**.
- Exponer rutas y recursos HTTP en **Api**; no devolver agregados de Domain directamente como contratos externos.
- Acceder a datos de otros Bounded Contexts mediante contratos/puertos, respetando su propiedad de datos.
- Crear las migraciones de EF Core desde Infrastructure, versionarlas en Git y **nunca generar nuevas migraciones solo por clonar el repositorio**.
- No compartir contraseñas, archivos de configuración personal, carpetas `bin/` o `obj/`.
- Actualizar este README cuando se agreguen endpoints, módulos o cambios relevantes de instalación.

---

**Proyecto:** VetPax · **Curso:** 1ASI0728 — Arquitecturas de Software Emergentes · **Estado:** desarrollo incremental.
