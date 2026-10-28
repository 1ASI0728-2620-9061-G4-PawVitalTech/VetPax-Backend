# VetPax Backend — Instalación local, base de datos, Keycloak y guía de la API

> **Guía para ejecutar VetPax sin desplegarlo en internet.**
> 
> **Estado de referencia:** código de `VetPax-Backend.zip` revisado y cuatro endpoints adicionales implementados y probados posteriormente (`GET /pets`, `GET /clinics`, `GET /clinics/{clinicId}/veterinarians` y `GET /appointments/availability`). Las modificaciones más recientes que estén solamente en tu repositorio local prevalecen sobre el ZIP. Este README describe el flujo local y no certifica despliegue en producción.
>
> **Público:** desarrolladores del backend y compañeros que clonarán el repositorio desde GitHub.

## Índice

1. [Qué es VetPax y cómo funciona](#1-qué-es-vetpax-y-cómo-funciona)
2. [Requisitos previos](#2-requisitos-previos)
3. [Clonar el repositorio](#3-clonar-el-repositorio)
4. [Instalar y configurar MySQL](#4-instalar-y-configurar-mysql)
5. [Levantar Keycloak con Docker](#5-levantar-keycloak-con-docker) — incluye **respaldo y restauración al clonar** en la sección 5.5
6. [Configurar Keycloak, clientes y usuarios](#6-configurar-keycloak-clientes-y-usuarios)
7. [Configurar secretos de la API](#7-configurar-secretos-de-la-api)
8. [Aplicar las migraciones de EF Core](#8-aplicar-las-migraciones-de-ef-core)
9. [Compilar, ejecutar y abrir Swagger](#9-compilar-ejecutar-y-abrir-swagger)
10. [Autenticación y roles](#10-autenticación-y-roles)
11. [Catálogo completo de endpoints](#11-catálogo-completo-de-endpoints)
12. [Ejemplos de peticiones por módulo](#12-ejemplos-de-peticiones-por-módulo)
13. [Flujo completo de prueba local](#13-flujo-completo-de-prueba-local)
14. [Workers y servicios en segundo plano](#14-workers-y-servicios-en-segundo-plano)
15. [Pruebas automatizadas](#15-pruebas-automatizadas)
16. [Errores frecuentes y soluciones](#16-errores-frecuentes-y-soluciones)
17. [Buenas prácticas, respaldo y seguridad](#17-buenas-prácticas-respaldo-y-seguridad)
18. [Estado y límites de la implementación](#18-estado-y-límites-de-la-implementación)

---

## 1. Qué es VetPax y cómo funciona

**VetPax** es una solución para gestionar mascotas, clínicas veterinarias, citas, atenciones clínicas, prescripciones de medicación y alimentación, y seguimiento de constancia en los cuidados. El backend es una **API REST construida con ASP.NET Core sobre .NET 10**.

Se utiliza de tres maneras:

- **Propietario (`owner`):** normalmente desde la futura aplicación móvil. Puede registrar y consultar sus mascotas, vincularlas a clínicas, consultar disponibilidad, agendar citas, ver indicaciones y registrar administraciones de medicamentos.
- **Veterinario (`veterinarian`):** desde la plataforma destinada al personal clínico. Consulta pacientes, agenda y evolución, registra atenciones y prescribe tratamientos.
- **Administrador de clínica (`clinic_administrator`):** gestiona los datos de su clínica y la vinculación de veterinarios.

**Swagger solo es una herramienta de desarrollo**: iniciar sesión con `owner` en Swagger para probar una ruta no implica que el propietario vaya a tener acceso a la plataforma web.

### 1.1. Arquitectura

VetPax utiliza un **monolito modular**, DDD y arquitectura hexagonal:

```text
Aplicación móvil (owner)     Plataforma web (personal)
            \                       /
             \  HTTPS / JSON / JWT  /
              v                 v
                   VetPax.Api
                       |
               VetPax.Application
                       |
                  VetPax.Domain
                       ^
                       |
              VetPax.Infrastructure
                 |          |
             MySQL      Adaptadores
                         |       |
                      Keycloak  Firebase (opcional)
```

| Proyecto | Función |
|---|---|
| `VetPax.Api` | Controladores HTTP, Swagger, autenticación y servicios hospedados. |
| `VetPax.Application` | Casos de uso, handlers, contratos y puertos. |
| `VetPax.Domain` | Entidades, value objects, reglas de negocio y eventos. |
| `VetPax.Infrastructure` | Persistencia EF Core/MySQL, adaptadores y conectores. |
| `tests/VetPax.Tests` | Pruebas unitarias, de aplicación y algunas de integración. |
| `infra/keycloak` | Docker Compose de Keycloak y PostgreSQL, más archivos de importación. |

**Bounded contexts:** `IdentityAccessManagement`, `ClinicManagement`, `PetClinicalCare`, `AppointmentManagement`, `MedicationTreatment`, `NutritionManagement` y `AdherenceGamification`.

**Servicios locales separados:**

| Servicio | Dirección predeterminada | Persistencia |
|---|---|---|
| API VetPax | `https://localhost:7175` y `http://localhost:5139` | MySQL `vetpax_db` |
| Swagger | `https://localhost:7175/swagger` | No aplica |
| OpenAPI JSON | `https://localhost:7175/openapi/v1.json` | No aplica |
| Keycloak | `http://localhost:8080` | PostgreSQL del Docker Compose |
| Administración Keycloak | `http://localhost:8080/admin/` | PostgreSQL del Docker Compose |
| MySQL | `localhost:3306` (configurable) | Base `vetpax_db` |

> **No confundas las bases de datos:** las tablas de negocio de VetPax están en **MySQL**; Keycloak administra sus usuarios, clientes y sesiones en su propio **PostgreSQL** de Docker. Las migraciones de EF Core solo modifican MySQL.

## 2. Requisitos previos

Instala los siguientes componentes en el equipo de desarrollo:

| Requisito | Versión o criterio |
|---|---|
| Git | Para clonar y actualizar el repositorio. |
| **.NET SDK** | **10**, porque los proyectos usan `net10.0`. |
| Docker Desktop + Docker Compose | Para Keycloak y su PostgreSQL; opcionalmente también para MySQL. |
| MySQL Server | **8.x** (local o en contenedor). |
| JetBrains Rider, Visual Studio o VS Code | Cualquiera permite trabajar con la solución. |
| Navegador | Para Keycloak y Swagger. |
| `dotnet-ef` | Herramienta de EF Core **9.x**, compatible con las dependencias del proyecto. |

Verifica en PowerShell:

```powershell
git --version
dotnet --version
docker --version
docker compose version
```

En Windows, si es la primera vez que usas HTTPS local con ASP.NET Core:

```powershell
dotnet dev-certs https --trust
```

Acepta la confirmación del certificado de desarrollo cuando Windows lo solicite.

## 3. Clonar el repositorio

Abre PowerShell en la carpeta donde guardas tus proyectos:

```powershell
git clone URL_REAL_DEL_REPOSITORIO
cd VetPax-Backend
```

**Sustituye** `URL_REAL_DEL_REPOSITORIO` por la URL de GitHub que comparta el equipo. No se fija aquí una URL no verificada. Si el nombre de la carpeta clonada es otro, adapta el `cd`.

Comprueba que estás en la raíz correcta:

```powershell
dir
```

Deberías ver, entre otros:

```text
VetPax-Backend.sln
VetPax.Api/
VetPax.Application/
VetPax.Domain/
VetPax.Infrastructure/
infra/keycloak/
tests/
```

**Importante:** las rutas correctas de este repositorio **NO comienzan con `src/`**. Algunos documentos antiguos tenían esa ruta y están desactualizados.

Obtén posteriormente los cambios del equipo con:

```powershell
git pull
```

Antes de hacer `git pull`, guarda o confirma tus cambios locales para evitar conflictos.

## 4. Instalar y configurar MySQL

### 4.1. Opción A: MySQL Server instalado en Windows

1. Inicia el servicio MySQL desde Windows o MySQL Workbench.
2. Conéctate al servidor local (`localhost:3306`).
3. En MySQL Workbench, abre una consulta y ejecuta:

```sql
CREATE DATABASE IF NOT EXISTS vetpax_db
  CHARACTER SET utf8mb4
  COLLATE utf8mb4_unicode_ci;
```

Para desarrollo, puedes usar un usuario dedicado con privilegios de escritura y de modificación de esquema necesarios para las migraciones:

```sql
CREATE USER IF NOT EXISTS 'vetpax_app'@'localhost'
  IDENTIFIED BY 'CAMBIAR_POR_UNA_CONTRASENA_LOCAL_SEGURA';
GRANT ALL PRIVILEGES ON vetpax_db.* TO 'vetpax_app'@'localhost';
```

> Cambia esa contraseña antes de ejecutar el SQL; **no uses el texto de ejemplo en un entorno real**. `GRANT ALL` sobre **esta base de desarrollo** facilita aplicar migraciones, pero no es una política de privilegios recomendada para producción.

### 4.2. Opción B: MySQL con Docker (opcional)

Si prefieres no instalar MySQL en Windows, crea un contenedor aparte. Ejemplo ilustrativo (cambia la contraseña):

```powershell
docker volume create vetpax_mysql_data

docker run -d `
  --name vetpax-mysql `
  -p 127.0.0.1:3306:3306 `
  -e MYSQL_ROOT_PASSWORD=CAMBIAR_PASSWORD_LOCAL `
  -e MYSQL_DATABASE=vetpax_db `
  -v vetpax_mysql_data:/var/lib/mysql `
  mysql:8.0
```

> La contraseña del ejemplo puede quedar visible en el historial de PowerShell; para un entorno compartido usa un archivo de entorno privado o un mecanismo seguro de secretos. **No ejecutes un segundo MySQL en el mismo puerto 3306** si ya tienes el servidor instalado.

Comandos de comprobación:

```powershell
docker ps
docker logs vetpax-mysql
```

**Elige A o B, no necesitas ambas.**

### 4.3. Las migraciones NO restauran datos anteriores

Crear `vetpax_db` y ejecutar `database update` reconstruye el **esquema** (tablas, índices, relaciones), pero **no importa los registros de prueba** que otra persona tenga guardados.

Esto significa que, en una instalación limpia, no aparecerán automáticamente la mascota Max, sus citas, ni los IDs utilizados durante nuestras pruebas anteriores. Para transferir datos existentes necesitas un **respaldo SQL** de MySQL además de las migraciones (se explica en el apartado 17).

## 5. Levantar Keycloak con Docker

> **¿Eres un compañero que acaba de clonar VetPax y recibió un respaldo de Keycloak?** Sigue directamente la [sección 5.5: instalación desde GitHub con el respaldo compartido](#55-compartir-keycloak-con-el-equipo-respaldo-y-restauración-al-clonar). **No ejecutes `docker compose up -d` con ambos servicios antes de restaurar**. Primero se inicia solamente `keycloak-db`.

VetPax usa la siguiente estructura local:

```text
infra/keycloak/compose.yaml
infra/keycloak/.env.example
infra/keycloak/import/vetpax-realm.json       # si se distribuye un export del realm
infra/keycloak/import/vetpax-users-0.json     # si se distribuye un export de usuarios
infra/keycloak/backup/vetpax-keycloak.dump    # solo si se recibe el respaldo privado
```

**Atención al clonar:** `infra/keycloak/.gitignore` excluye `import/`, `backup/` y `.env`. Esos archivos **no están garantizados en un clon de GitHub**. Para reproducir los usuarios y configuraciones reales de tu equipo, utiliza el respaldo privado de la **sección 5.5**; no dependas de encontrar los JSON de `import/` en el repositorio.

### 5.1. Preparar variables privadas de Docker

Desde la raíz del repositorio:

```powershell
cd .\infra\keycloak
Copy-Item .env.example .env
notepad .env
```

En `.env`, cambia las contraseñas de ejemplo por valores de desarrollo privados:

```dotenv
KEYCLOAK_ADMIN_USERNAME=admin
KEYCLOAK_ADMIN_PASSWORD=CAMBIAR_CONTRASENA_ADMIN_LOCAL
KEYCLOAK_DB_PASSWORD=CAMBIAR_CONTRASENA_POSTGRES_LOCAL
```

**No publiques `.env` ni lo subas a Git.** El archivo `.env.example` solo sirve de plantilla.

### 5.2. Iniciar los contenedores

Este procedimiento es para **crear un entorno Keycloak desde el import del realm** o **volver a iniciar un entorno que ya tiene su base restaurada**. Si acabas de clonar el repositorio y vas a importar el `.dump` de tu compañero, utiliza la **sección 5.5**, no este primer arranque.

Desde `infra/keycloak`:

```powershell
docker compose up -d
docker compose ps
```

Compose inicia **dos contenedores**:

- `keycloak-db`: PostgreSQL 17, exclusivamente para Keycloak.
- `keycloak`: Keycloak 26.8.1, expuesto localmente en `http://localhost:8080`.

Verifica los logs:

```powershell
docker compose logs --tail=100 keycloak
```

Abre:

- Consola: **http://localhost:8080/admin/**
- Realm: **http://localhost:8080/realms/vetpax**

Inicia sesión en la consola con el administrador de **Keycloak** definido en `.env`. **Ese administrador de Keycloak no es lo mismo que un usuario VetPax con rol `clinic_administrator`.**

### 5.3. Importación automática del realm

El Compose ejecuta Keycloak con `start-dev --import-realm` y monta `infra/keycloak/import/` como fuente de importación. En una **base PostgreSQL nueva**, puede importar el realm `vetpax` con sus roles y clientes **si los JSON de exportación están realmente presentes** en `infra/keycloak/import/`. Si la carpeta está vacía, no se recuperará el realm de VetPax: para el respaldo compartido usa la sección 5.5.

El export incluido declara los roles:

- `owner`
- `veterinarian`
- `clinic_administrator`

Y los clientes relevantes:

| Cliente | Finalidad |
|---|---|
| `vetpax-api` | Identificador de audiencia JWT que valida la API: `vetpax-api`. |
| `vetpax-dev` | Cliente público para pruebas OAuth Authorization Code + PKCE, sin secreto. |
| `vetpax-admin` | Cliente **confidencial** con service account, utilizado por la API para registrar propietarios. Su secreto es solo de servidor. |

La importación también puede incluir usuarios de ejemplo del export. **No confíes en que sean las cuentas de tu equipo ni reutilices sus credenciales**; revisa los usuarios y cambia o elimina datos de demostración cuando corresponda.

> Importar un realm **no sobrescribe automáticamente** una configuración ya existente en PostgreSQL. Si Keycloak ya se usó antes, los ajustes manuales del realm pueden ser distintos del JSON versionado. Verifica la configuración en la consola antes de intentar reinicializar volúmenes.

### 5.4. Apagar y volver a iniciar Keycloak

```powershell
# Desde infra/keycloak:
docker compose down       # Detiene contenedores; conserva PostgreSQL
docker compose up -d     # Los vuelve a iniciar con sus usuarios/datos
```

**No ejecutes `docker compose down -v`** para un reinicio normal: esa opción elimina los volúmenes y puede borrar los usuarios, clientes y sesiones de Keycloak.

Vuelve a la raíz del backend:

```powershell
cd ..\..
```

### 5.5. Compartir Keycloak con el equipo: respaldo y restauración al clonar

**Caso de uso:** un integrante ya configuró el realm `vetpax` en Docker y quiere que los demás, tras clonar GitHub, arranquen **una copia local e independiente** de esa instalación. Se conservarán los **IDs de usuario (`sub`)**, cuentas, hashes de contraseñas, roles, clientes, configuraciones y permisos almacenados en PostgreSQL en el momento del respaldo. **Cada integrante ejecutará Docker en su propio equipo:** no se conectará a la PC de otro.

> Este procedimiento es **exclusivo para un entorno de desarrollo** y debe realizarse con autorización de las personas cuyos datos puedan estar en el respaldo. Un dump de Keycloak contiene datos personales y secretos de autenticación. **Nunca subirlo a GitHub, publicarlo en un enlace abierto ni enviarlo en un chat público.** Para compartirlo, utilizar un canal privado y cifrado. Si existen cuentas reales, sustituirlas por cuentas ficticias antes de preparar el paquete de desarrollo.

#### 5.5.1. Preparación: quien ya tiene Keycloak funcionando

**Solo la persona que actualmente tiene Keycloak configurado** ejecuta estos comandos. Abre **PowerShell** desde la raíz `VetPax-Backend` y comprueba que Docker Desktop está iniciado:

```powershell
cd .\infra\keycloak
docker compose ps
```

Deben estar funcionando `keycloak-db` y `keycloak`. **No es necesario detener Keycloak para realizar el `pg_dump`.** Crea una carpeta local para el respaldo:

```powershell
New-Item -ItemType Directory -Force .\backup
```

Genera el dump comprimido de la base PostgreSQL de Keycloak **dentro del contenedor**:

```powershell
docker compose exec -T keycloak-db `
  pg_dump -U keycloak -d keycloak -Fc `
  -f /tmp/vetpax-keycloak.dump
```

Copia el archivo de Docker a Windows:

```powershell
docker compose cp `
  keycloak-db:/tmp/vetpax-keycloak.dump `
  .\backup\vetpax-keycloak.dump
```

Comprueba su existencia y que el formato sea reconocible:

```powershell
Get-Item .\backup\vetpax-keycloak.dump

docker compose exec -T keycloak-db `
  pg_restore -l /tmp/vetpax-keycloak.dump
```

Genera un hash de integridad opcional para compartirlo por un canal diferente al respaldo:

```powershell
Get-FileHash .\backup\vetpax-keycloak.dump -Algorithm SHA256
```

**Entrega al equipo** el archivo `vetpax-keycloak.dump` por un medio privado y la URL de GitHub del repositorio. **No envíes `.env`**; cada integrante crea el suyo desde `.env.example`. El respaldo **no se añade a Git**, pues `infra/keycloak/.gitignore` excluye `backup/`.

> Un `pg_restore -l` exitoso comprueba que el dump tiene una estructura legible, pero **no sustituye la verificación de una restauración completa** en otra instalación.

#### 5.5.2. Instalación del compañero: clonar y preparar Docker

En una computadora **nueva para este proyecto**, instala Git, Docker Desktop y .NET 10 (sección 2). Abre PowerShell en la carpeta de trabajo:

```powershell
# Sustituir por la URL real que compartió el equipo.
git clone URL_REAL_DEL_REPOSITORIO
cd .\VetPax-Backend
```

Confirma que existen `infra/keycloak/compose.yaml` y `.env.example`. Prepara tus variables privadas:

```powershell
cd .\infra\keycloak
Copy-Item .\.env.example .\.env
notepad .\.env
```

En `.env`, configura **una contraseña local nueva de PostgreSQL** y las variables esperadas por Compose, por ejemplo:

```dotenv
KEYCLOAK_ADMIN_USERNAME=admin
KEYCLOAK_ADMIN_PASSWORD=VALOR_LOCAL_PARA_BOOTSTRAP
KEYCLOAK_DB_PASSWORD=CONTRASENA_LOCAL_PRIVADA_PARA_POSTGRES
```

**Aclaración:** la contraseña de `KEYCLOAK_DB_PASSWORD` puede ser distinta de la del equipo origen, porque la base se restaurará en un PostgreSQL nuevo. En cambio, **el administrador de Keycloak restaurado mantiene la contraseña de su propia cuenta almacenada en el dump**; modificar `KEYCLOAK_ADMIN_PASSWORD` no la resetea si ese usuario ya existe.

Crea la carpeta de respaldo y coloca allí el archivo recibido por el canal privado:

```powershell
New-Item -ItemType Directory -Force .\backup
# Colocar manualmente el archivo recibido en:
# infra/keycloak/backup/vetpax-keycloak.dump
Get-Item .\backup\vetpax-keycloak.dump
```

**No necesitas copiar una imagen Docker ni el volumen original del compañero:** `compose.yaml` descarga las imágenes y crea un volumen PostgreSQL nuevo en tu equipo. El respaldo contiene los datos a restaurar.

#### 5.5.3. Restaurar sobre PostgreSQL VACÍO (sin iniciar Keycloak todavía)

**Requisito obligatorio:** este procedimiento supone que el volumen `vetpax-identity_keycloak_pgdata` o equivalente está **recién creado y sin una base Keycloak poblada**. No lo ejecutes sobre una instalación existente con usuarios o datos que quieras conservar: podría provocar errores o mezclar esquemas. **No uses `docker compose down -v` como solución automática**, porque elimina volúmenes con datos.

Desde `infra/keycloak`, inicia **solamente PostgreSQL**:

```powershell
docker compose up -d keycloak-db
docker compose ps
```

Copia el dump recibido al contenedor nuevo:

```powershell
docker compose cp `
  .\backup\vetpax-keycloak.dump `
  keycloak-db:/tmp/vetpax-keycloak.dump
```

Comprueba que PostgreSQL puede leerlo y restaura:

```powershell
docker compose exec -T keycloak-db `
  pg_restore -l /tmp/vetpax-keycloak.dump

docker compose exec -T keycloak-db `
  pg_restore -U keycloak -d keycloak `
  --no-owner --no-acl --exit-on-error `
  /tmp/vetpax-keycloak.dump
```

**Si `pg_restore` termina con errores, no arranques Keycloak todavía.** Revisa el error, la versión PostgreSQL (la de Compose es 17), el archivo y si la base ya estaba poblada. No repitas la restauración encima de una restauración parcial sin decidir cómo recuperar el estado limpio.

#### 5.5.4. Arrancar Keycloak y verificar usuarios y clientes

Solo **después de restaurar sin errores**, inicia Keycloak:

```powershell
docker compose up -d keycloak
docker compose ps
docker compose logs --tail=100 keycloak
```

Abre **http://localhost:8080/admin/** y verifica el realm **`vetpax`**. Para entrar como administrador necesitarás **las credenciales del administrador que existían en el origen cuando se hizo el dump**. Que el `.env` de destino tenga otra contraseña no implica que la cuenta restaurada cambie.

Verificaciones mínimas:

1. En **Realm roles** aparecen `owner`, `veterinarian` y `clinic_administrator`.
2. En **Clients** están `vetpax-api`, `vetpax-dev`, `vetpax-admin` y, si fue creado en la instalación original, `vetpax-swagger`.
3. En **Users** aparecen las cuentas incluidas en el momento del respaldo. **No hace falta volver a registrarlas.**
4. La URL **http://localhost:8080/realms/vetpax/.well-known/openid-configuration** responde con la configuración OpenID Connect del realm.
5. Al ejecutar `GET /api/v1/auth/me` y `GET /api/v1/auth/me/permissions` desde Swagger, los roles se resuelven correctamente.

El `compose.yaml` de desarrollo publica Keycloak en **`127.0.0.1:8080`**. Esto es deliberado: cada copia es accesible desde la computadora que la ejecuta, **no es un servidor compartido accesible por internet**. El `KC_HOSTNAME` está configurado para `http://localhost:8080`; si se decide utilizar un host o puerto distinto, se debe actualizar la configuración OAuth y el issuer de los tokens, no solo el puerto.

#### 5.5.5. Conectar el backend VetPax del compañero

Vuelve a la raíz del repositorio:

```powershell
cd ..\..
```

A continuación:

1. Prepara **MySQL** y crea `vetpax_db` según la **sección 4**.
2. Si quieres **tablas vacías**, ejecuta las migraciones EF Core de la **sección 8**. Si también recibiste un dump **completo de MySQL** con datos y tabla `__EFMigrationsHistory`, restaura ese dump en una base nueva en lugar de recrear las mismas tablas encima.
3. Configura los **.NET User Secrets** de la **sección 7** (`ConnectionStrings:VetPaxDb` y `Keycloak:AdminClientSecret`). Este último debe coincidir con el secreto del cliente confidencial `vetpax-admin` restaurado; **no es** la contraseña del administrador de la consola de Keycloak.
4. Confirma que el issuer/realm de Keycloak sigue apuntando a `http://localhost:8080/realms/vetpax`, que la audiencia del JWT coincide con `vetpax-api` y que Swagger tiene un cliente público con las redirecciones permitidas (sección 6.2).
5. Compila e inicia la API según la **sección 9**.

Comandos típicos desde la raíz:

```powershell
dotnet restore .\VetPax-Backend.sln
dotnet build .\VetPax-Backend.sln

# Ejecutar solo cuando MySQL sea una base nueva sin las tablas ya restauradas.
dotnet ef database update `
  --project .\VetPax.Infrastructure\VetPax.Infrastructure.csproj `
  --startup-project .\VetPax.Api\VetPax.Api.csproj

dotnet run --project .\VetPax.Api\VetPax.Api.csproj --launch-profile https
```

Abre **https://localhost:7175/swagger** y autentícate con las cuentas de desarrollo del realm restaurado.

#### 5.5.6. Qué queda igual y qué NO restaura este respaldo

| Elemento | ¿Se restaura con `vetpax-keycloak.dump`? |
|---|---|
| Realm `vetpax`, clientes y roles | Sí, si existían al crear el dump. |
| Usuarios, identificadores Keycloak y hashes de contraseñas | Sí, si estaban en ese PostgreSQL. |
| Secretos de clientes confidenciales y permisos de service accounts | Sí, según el estado al respaldar. |
| Clínica, Max, citas, medicaciones y planes alimenticios | **No:** esos registros están en **MySQL**, no en Keycloak. |
| Tablas de negocio de VetPax | **No:** las crean las migraciones de EF Core o se recuperan con un dump MySQL. |
| Tokens FCM reales, archivos locales externos a PostgreSQL y configuración del teléfono | **No.** |

**Mantener identificadores consistentes:** si un compañero restaura también la MySQL original, debe utilizar una copia compatible del **mismo estado de usuarios de Keycloak**, porque tablas como `pets` o `appointments` guardan identificadores vinculados a los `sub` de Keycloak. Si decide crear MySQL desde cero, podrá iniciar sesión con las cuentas restauradas, pero no verá a Max ni las citas hasta que vuelva a crearlas o importe un respaldo de datos.

**Respaldo periódico:** cada vez que agregues usuarios, modifiques roles, clientes, redirecciones o secretos de Keycloak, el dump anterior queda desactualizado. Si quieres compartir los cambios con el equipo, crea un **nuevo dump** y acuerda cómo restaurarlo; **no lo importes directamente sobre las bases ya pobladas de tus compañeros**.

---

## 6. Configurar Keycloak, clientes y usuarios

### 6.1. Verificar el realm

Desde la consola de administración de Keycloak:

1. Selecciona el realm **`vetpax`**.
2. En **Realm roles**, comprueba `owner`, `veterinarian` y `clinic_administrator`.
3. En **Clients**, comprueba `vetpax-api`, `vetpax-dev` y `vetpax-admin`.
4. Comprueba que el usuario que probará la API tiene **exactamente uno** de los tres roles de VetPax; el adaptador de identidad del código revisado espera un rol de aplicación inequívoco.

### 6.2. Configurar el cliente de Swagger

El `Program.cs` del ZIP utiliza:

```csharp
options.OAuthClientId("vetpax-swagger");
options.OAuthUsePkce();
```

El export original del realm **no incluye** ese cliente. Para que el botón **Authorize** de Swagger funcione tal como está configurado el código, crea en Keycloak un cliente:

| Opción | Valor |
|---|---|
| Client ID | `vetpax-swagger` |
| Client authentication | **Off** (cliente público) |
| Standard flow | **On** |
| PKCE | **S256** |
| Valid redirect URI | `https://localhost:7175/swagger/oauth2-redirect.html` |
| Web origin | `https://localhost:7175` |

También puedes adaptar el cliente de prueba existente, pero **el Client ID de Swagger y el de Keycloak deben coincidir**, y la URI de redirección debe estar autorizada. No coloques el secreto de `vetpax-admin` en Swagger ni en una aplicación móvil.

**Alternativa Postman:** usar `vetpax-dev` como cliente público, flujo **Authorization Code with PKCE**, scopes `openid profile email` y una de las URI de callback de Postman autorizadas en el realm.

### 6.3. Dar acceso administrativo al backend

El endpoint `POST /api/v1/auth/register` crea cuentas `owner` mediante la API de administración de Keycloak. Por eso, `vetpax-admin` debe disponer de:

- **Service accounts enabled**.
- Un **client secret** válido que solo conocerá el backend.
- Permisos de servicio suficientes, como la asignación `realm-management` → `manage-users` y los permisos de lectura de roles correspondientes. El export revisado incluye permisos de servicio; verifícalos en tu instalación.

En Keycloak, abre **Clients → vetpax-admin → Credentials** y copia el secreto **solo a la configuración privada local** descrita en el siguiente apartado.

### 6.4. Crear las tres identidades para pruebas

> **Si restauraste un respaldo completo mediante la sección 5.5, no vuelvas a crear las mismas cuentas:** ya están en Keycloak con sus identificadores originales. Este procedimiento de creación manual es para una instalación inicial sin dichos usuarios.

| Tipo de usuario | Cómo crearlo | Rol que debe tener |
|---|---|---|
| Propietario | `POST /api/v1/auth/register` (recomendado para probar el flujo); o en Keycloak si se necesita una cuenta de prueba. | `owner` |
| Veterinario | Desde Keycloak Admin → Users → Create user; establece contraseña y asigna el rol. | `veterinarian` |
| Administrador de clínica | Desde Keycloak Admin → Users → Create user; establece contraseña y asigna el rol. | `clinic_administrator` |

Para veterinario y administrador:

1. Crea el usuario y habilítalo.
2. Configura una contraseña permanente para desarrollo (quita la obligación de cambiarla al primer inicio, si procede).
3. En **Role mapping**, asigna el rol correcto del realm.
4. Entra desde Swagger con cada identidad y comprueba `GET /api/v1/auth/me` y `GET /api/v1/auth/me/permissions`.

**La relación veterinario-clínica no se crea en Keycloak.** Después hay que vincular el ID del veterinario a una clínica usando `POST /api/v1/clinics/{clinicId}/veterinarians` con una sesión de `clinic_administrator`.

> Los identificadores (`sub` de Keycloak) de los usuarios deben mantenerse estables si restauras una base MySQL existente. Importar una base con referencias a IDs que no existen en el realm nuevo puede romper las relaciones de autorización.

## 7. Configurar secretos de la API

Desde la **raíz** del repositorio, configura **.NET User Secrets** en el proyecto `VetPax.Api`. El `.csproj` ya contiene un `UserSecretsId`; no necesitas ejecutar `dotnet user-secrets init`.

**Con MySQL instalado localmente** (ajusta usuario y contraseña):

```powershell
dotnet user-secrets set "ConnectionStrings:VetPaxDb" "Server=127.0.0.1;Port=3306;Database=vetpax_db;User=vetpax_app;Password=CAMBIAR_PASSWORD;" --project .\VetPax.Api\VetPax.Api.csproj
```

**Si MySQL está en Docker y usas root para una prueba aislada**, cambia `User` por `root` y coloca la contraseña del contenedor. En una instalación de equipo se prefiere usuario propio de desarrollo.

El secreto del cliente de servicio de Keycloak:

```powershell
dotnet user-secrets set "Keycloak:AdminClientSecret" "PEGAR_SECRETO_PRIVADO_DE_VETPAX_ADMIN" --project .\VetPax.Api\VetPax.Api.csproj
```

> Los valores entre comillas son ejemplos: reemplázalos. Evita publicar capturas con contraseñas, salidas de `user-secrets list`, JWT o archivos de credenciales. Las órdenes `user-secrets set` que contienen el secreto también pueden quedar en el historial local de la terminal; para un entorno sensible, usa variables de entorno introducidas de forma segura.

Los demás valores de Keycloak que ya están en `VetPax.Api/appsettings.json` son:

```json
{
  "Keycloak": {
    "BaseUrl": "http://localhost:8080",
    "Realm": "vetpax",
    "Audience": "vetpax-api",
    "AdminClientId": "vetpax-admin"
  }
}
```

No es necesario duplicarlos en User Secrets si ejecutas Keycloak en la ubicación predeterminada.

### 7.1. Alternativa: variables de entorno

ASP.NET Core permite usar `__` para representar `:` en claves jerárquicas. Ejemplos de nombres:

```text
ConnectionStrings__VetPaxDb
Keycloak__AdminClientSecret
Firebase__ProjectId
GOOGLE_APPLICATION_CREDENTIALS
```

**`GOOGLE_APPLICATION_CREDENTIALS`** se usa solo cuando sea necesario acceder a Firebase con credenciales de servicio; apunta a un archivo JSON **fuera del repositorio**.

## 8. Aplicar las migraciones de EF Core

Las migraciones de VetPax **ya están en Git** bajo:

```text
VetPax.Infrastructure/Persistence/Migrations/
```

Incluyen, en orden:

1. `InitialPetClinicalCare`
2. `AddClinicManagement`
3. `AddPetClinicLinks`
4. `AddAppointmentManagement`
5. `AddMedicationTreatment`
6. `AddMedicationDeviceTokens`
7. `AddNutritionManagement`
8. `AddNutritionReminderProcessing`
9. `AddAdherenceGamification`
10. `AddAdherenceOutboxDelivery`

### 8.1. Restaurar y compilar

Desde la raíz:

```powershell
dotnet restore .\VetPax-Backend.sln
dotnet build .\VetPax-Backend.sln -c Debug
```

### 8.2. Instalar la herramienta EF Core

Si no tienes `dotnet ef`:

```powershell
dotnet tool install --global dotnet-ef --version 9.0.0
```

Si ya está instalada, comprueba su versión con:

```powershell
dotnet ef --version
```

### 8.3. Ver migraciones existentes

```powershell
dotnet ef migrations list --project .\VetPax.Infrastructure\VetPax.Infrastructure.csproj --startup-project .\VetPax.Api\VetPax.Api.csproj
```

### 8.4. Crear todas las tablas de una base vacía

```powershell
dotnet ef database update --project .\VetPax.Infrastructure\VetPax.Infrastructure.csproj --startup-project .\VetPax.Api\VetPax.Api.csproj
```

Esto aplica las migraciones pendientes; **no crea nuevas migraciones**. No ejecutes `dotnet ef migrations add` cuando solo estás clonando el repositorio.

**El `Program.cs` también llama a `Database.MigrateAsync()` cuando el entorno es `Development`.** Por tanto, al iniciar la API local, intentará aplicar automáticamente las migraciones pendientes. Puedes ejecutar `database update` primero para identificar más claramente los errores de conexión.

### 8.5. Verificar que se crearon las tablas

En MySQL Workbench:

```sql
USE vetpax_db;
SHOW TABLES;

SELECT MigrationId, ProductVersion
FROM __EFMigrationsHistory
ORDER BY MigrationId;
```

La tabla `__EFMigrationsHistory` registra las migraciones aplicadas. También verás tablas como `pets`, `appointments`, `medication_plans`, `medication_doses` y `nutrition_feeding_plans`, además de las de clínica, historia clínica y gamificación.

**Si una migración ya está aplicada, EF Core no debe volver a ejecutarla**. No elimines `__EFMigrationsHistory` para “repetir” el proceso.

## 9. Compilar, ejecutar y abrir Swagger

Con MySQL y Keycloak funcionando y los secretos configurados, desde la raíz:

```powershell
dotnet run --project .\VetPax.Api\VetPax.Api.csproj --launch-profile https
```

Una ejecución correcta mostrará mensajes similares a:

```text
Now listening on: https://localhost:7175
Now listening on: http://localhost:5139
Hosting environment: Development
```

Abre **https://localhost:7175/swagger**.

> Después de modificar código fuente **NO utilices `--no-build`**: ejecutarías binarios antiguos. Si hay errores de compilación, corrígelos antes de abrir Swagger.

### 9.1. Ejecutar desde Rider

1. Abre `VetPax-Backend.sln`.
2. Espera a la restauración de paquetes NuGet.
3. Selecciona el proyecto `VetPax.Api` y el perfil **https**.
4. Ejecuta.
5. Abre Swagger en el navegador.

### 9.2. Detener los servicios

- API: presiona **Ctrl+C** en la terminal de `dotnet run`, o detén la ejecución desde Rider.
- Keycloak: `docker compose down` desde `infra/keycloak`.
- MySQL Docker opcional: `docker stop vetpax-mysql`; para iniciar luego, `docker start vetpax-mysql`.
- MySQL instalado en Windows: normalmente se mantiene como servicio local.

## 10. Autenticación y roles

### 10.1. Autenticarse en Swagger

1. Abre Swagger en `https://localhost:7175/swagger`.
2. Pulsa **Authorize**.
3. Usa el flujo **OAuth2 Authorization Code + PKCE** configurado para el cliente público `vetpax-swagger`.
4. Inicia sesión con el usuario del rol que corresponda a la prueba.
5. Ejecuta `GET /api/v1/auth/me` para confirmar el identificador y rol.
6. Ejecuta `GET /api/v1/auth/me/permissions` para confirmar los permisos.

**No necesitas copiar manualmente JWT dentro de cada solicitud** si Swagger ya obtuvo la autorización. Los tokens caducan: un `401` puede requerir volver a autorizarse.

### 10.2. Matriz básica de permisos

| Rol | Permisos devueltos por `/auth/me/permissions` |
|---|---|
| `owner` | `ManageOwnPets`, `ViewOwnClinicalHistory`, `ScheduleOwnAppointments` |
| `veterinarian` | `ViewAuthorizedPatients`, `RegisterClinicalAttention`, `ViewClinicalEvolution` |
| `clinic_administrator` | `ManageClinic` |

Estos permisos son los observados en las pruebas locales. **Que el endpoint devuelva permisos no sustituye las pruebas de acceso denegado** y autorización por clínica/mascota.

### 10.3. Códigos HTTP habituales

| Código | Significado en VetPax |
|---|---|
| `200 OK` | Consulta o modificación exitosa. |
| `201 Created` | Recurso creado. |
| `204 No Content` | Operación exitosa sin cuerpo de respuesta. |
| `400 Bad Request` | Petición o dato inválido. |
| `401 Unauthorized` | No autenticado o token inválido/caducado. |
| `403 Forbidden` | Autenticado, pero sin permisos o sin autorización sobre el recurso. |
| `404 Not Found` | Recurso no encontrado o ningún plan activo. |
| `409 Conflict` | Conflicto de negocio o concurrencia (debe diagnosticarse si es inesperado). |
| `429 Too Many Requests` | Límite del registro de usuarios excedido. |
| `502 Bad Gateway` | Error al comunicarse con un servicio de identidad externo en registro. |

## 11. Catálogo completo de endpoints

**Base:** `https://localhost:7175/api/v1`. **Total: 38 combinaciones de método + ruta** (34 del código fuente de referencia y los 4 GET incorporados y probados posteriormente).

En las tablas, `O` = owner, `V` = veterinarian, `A` = clinic_administrator. Los permisos adicionales de vinculación clínica/propiedad se validan mediante los casos de uso. Las tablas describen el rol **funcional**; la autorización precisa puede comprobarse en el código de cada operación.

### 11.1. Identity & Access Management — 3 rutas

| Método | Ruta | Rol | Para qué sirve |
|---|---|---|---|
| `POST` | `/auth/register` | Público | Crear propietario en Keycloak y asignar rol `owner`. |
| `GET` | `/auth/me` | Autenticado | Consultar la identidad del usuario del JWT. |
| `GET` | `/auth/me/permissions` | Autenticado | Consultar permisos basados en su rol. |

### 11.2. Clinic Management — 7 rutas

| Método | Ruta | Rol | Para qué sirve |
|---|---|---|---|
| `POST` | `/clinics` | A | Crear clínica y horario de atención. |
| `GET` | `/clinics` | O, V, A | **Nuevo:** listar clínicas por páginas (`page`, `pageSize`). |
| `GET` | `/clinics/{clinicId}` | V, A | Consultar perfil y horarios de una clínica autorizada. |
| `PUT` | `/clinics/{clinicId}` | A | Actualizar perfil y horarios de clínica. |
| `POST` | `/clinics/{clinicId}/veterinarians` | A | Vincular veterinario a clínica; recibe `veterinarianId`. |
| `GET` | `/clinics/{clinicId}/veterinarians` | O, V, A | **Nuevo:** listar identificadores de veterinarios activos. |
| `GET` | `/clinics/{clinicId}/patients` | V | Listar pacientes vinculados a la clínica. |

### 11.3. Pet & Clinical Care — 7 rutas

| Método | Ruta | Rol | Para qué sirve |
|---|---|---|---|
| `POST` | `/pets` | O | Registrar mascota y crear su historial. |
| `GET` | `/pets` | O | **Nuevo:** listar mascotas del propietario autenticado. |
| `GET` | `/pets/{petId}` | O | Consultar perfil de mascota propia. |
| `PUT` | `/pets/{petId}/clinics/{clinicId}` | O | Vincular mascota y clínica. |
| `GET` | `/pets/{petId}/clinical-records` | O | Consultar historial clínico de mascota propia. |
| `POST` | `/pets/{petId}/clinical-records?clinicId=...` | V | Registrar atención y sus indicadores; `appointmentId` opcional. |
| `GET` | `/pets/{petId}/evolution?clinicId=...` | V | Consultar evolución de indicadores en varias atenciones. |

### 11.4. Appointment Management — 5 rutas

| Método | Ruta | Rol | Para qué sirve |
|---|---|---|---|
| `POST` | `/appointments` | O | Agendar una cita de 30 minutos. |
| `GET` | `/appointments/{appointmentId}` | O, V | Consultar una cita. |
| `PATCH` | `/appointments/{appointmentId}` | O, V, según operación | Reprogramar, cancelar o marcar atendida. |
| `GET` | `/appointments/agenda?clinicId=...&from=...&to=...` | V | Consultar agenda por clínica y período. |
| `GET` | `/appointments/availability?clinicId=...&veterinarianId=...&date=...` | O | **Nuevo:** ofrecer horarios libres de 30 minutos. |

### 11.5. Medication Treatment — 9 rutas

| Método | Ruta | Rol | Para qué sirve |
|---|---|---|---|
| `POST` | `/pets/{petId}/medication-plans` | V | Prescribir plan de medicación. |
| `GET` | `/pets/{petId}/medication-plans` | O, V | Listar planes de medicación de mascota. |
| `GET` | `/medication-plans/{planId}` | O, V | Consultar plan individual. |
| `PATCH` | `/medication-plans/{planId}` | V | Cambiar horarios, completar o cancelar un plan. |
| `PATCH` | `/medication-plans/{planId}/reminders` | O | Activar o desactivar recordatorios. |
| `GET` | `/medication-plans/{planId}/doses?from=...&to=...` | O, V | Listar dosis generadas y sus estados. |
| `POST` | `/medication-doses/{doseId}/administrations` | O | Registrar administración de una dosis. |
| `POST` | `/me/medication-devices` | O | Registrar token de dispositivo para push. |
| `DELETE` | `/me/medication-devices/{deviceId}` | O | Deshabilitar un token/dispositivo. |

### 11.6. Nutrition Management — 6 rutas

| Método | Ruta | Rol funcional | Para qué sirve |
|---|---|---|---|
| `POST` | `/pets/{petId}/feeding-plans` | V | Prescribir plan alimenticio. |
| `GET` | `/pets/{petId}/feeding-plans/active` | O/V autorizado | Consultar el plan activo, si existe. |
| `GET` | `/feeding-plans/{planId}` | O/V autorizado | Consultar plan e historial de revisiones. |
| `PUT` | `/feeding-plans/{planId}` | V | Cambiar alimento, recomendaciones, fechas u horarios; crea revisión. |
| `PATCH` | `/feeding-plans/{planId}/reminders` | O | Activar/desactivar recordatorios. |
| `PATCH` | `/feeding-plans/{planId}/deactivate` | V | Desactivar plan y sus recordatorios. |

### 11.7. Adherence & Gamification — 1 ruta

| Método | Ruta | Rol | Para qué sirve |
|---|---|---|---|
| `GET` | `/me/adherence` | O | Consultar evidencias, puntuación, nivel y reconocimientos. |

**Conteo correcto de las tablas:** IAM 3 + Clínica 7 + Mascotas/Clínico 7 + Citas 5 + Medicación 9 + Nutrición 6 + Adherence 1 = **38 operaciones HTTP**.

## 12. Ejemplos de peticiones por módulo

Estos son **ejemplos de JSON**, no datos médicos reales. Utiliza siempre los IDs devueltos por tu instalación. Las fechas de ejemplo deben ajustarse para que estén vigentes cuando ejecutes las pruebas.

### 12.1. IAM: registrar propietario

`POST /api/v1/auth/register` — sin JWT:

```json
{
  "firstName": "Prueba",
  "lastName": "Local",
  "email": "propietario.prueba@example.com",
  "password": "CAMBIAR_POR_CONTRASENA_SEGURA"
}
```

- Requiere nombre y apellido de 2 a 80 caracteres, correo válido y contraseña de 8 a 128 caracteres.
- **`201`**: creado en Keycloak con rol `owner`.
- **`409`**: correo ya registrado.
- **`429`**: límite de solicitudes de registro (5 por minuto por IP en la política revisada).
- **`502`**: error de comunicación con Keycloak.

No publiques contraseñas reales en documentos, incidencias ni capturas.

`GET /api/v1/auth/me` y `GET /api/v1/auth/me/permissions` no reciben cuerpo; necesitan JWT.

### 12.2. Clínicas: crear, editar y vincular veterinarios

`POST /api/v1/clinics` — con `clinic_administrator`:

```json
{
  "name": "Veterinaria Demo",
  "address": "Lima, Peru - dirección de prueba",
  "phone": "010000000",
  "contactEmail": "contacto@example.com",
  "openingHours": [
    { "dayOfWeek": "Monday", "openTime": "09:00:00", "closeTime": "19:00:00" },
    { "dayOfWeek": "Tuesday", "openTime": "09:00:00", "closeTime": "19:00:00" },
    { "dayOfWeek": "Wednesday", "openTime": "09:00:00", "closeTime": "19:00:00" },
    { "dayOfWeek": "Thursday", "openTime": "09:00:00", "closeTime": "19:00:00" },
    { "dayOfWeek": "Friday", "openTime": "09:00:00", "closeTime": "19:00:00" }
  ]
}
```

La respuesta `201` incluye un nuevo `clinicId`. `PUT /api/v1/clinics/{clinicId}` utiliza el **mismo formato** para actualizar.

`POST /api/v1/clinics/{clinicId}/veterinarians` — admin:

```json
{
  "veterinarianId": "UUID_DEL_VETERINARIO_EN_KEYCLOAK"
}
```

La respuesta esperada es `204 No Content`. **No confundas este `POST` con el `GET` de la misma ruta**: el propietario puede consultar los veterinarios, pero no vincularlos.

Consultas sin cuerpo:

```http
GET /api/v1/clinics?page=1&pageSize=20
GET /api/v1/clinics/{clinicId}
GET /api/v1/clinics/{clinicId}/veterinarians
GET /api/v1/clinics/{clinicId}/patients
```

En el directorio de clínicas, `page` empieza en 1 y `pageSize` permite como máximo 50 registros según el nuevo handler. El directorio de veterinarios devuelve **IDs**, no nombres ni especialidades.

### 12.3. Mascotas: registrar, vincular y consultar

`POST /api/v1/pets` — con `owner`:

```json
{
  "name": "Max",
  "species": "Dog",
  "birthDate": "2020-03-15",
  "breed": "Golden Retriever",
  "sex": "Male"
}
```

`201` devuelve un `petId` nuevo y crea un historial clínico para esa mascota. El propietario se determina desde su JWT, **no** mediante un `ownerId` enviado por el cliente.

Consultas y vínculo:

```http
GET /api/v1/pets
GET /api/v1/pets/{petId}
PUT /api/v1/pets/{petId}/clinics/{clinicId}
GET /api/v1/pets/{petId}/clinical-records
```

El `PUT` de vínculo normalmente responde `204`; no necesita cuerpo. Para que el veterinario vea la mascota entre los pacientes de la clínica, primero deben existir la clínica, la mascota y el vínculo autorizado.

### 12.4. Atención clínica y evolución

`POST /api/v1/pets/{petId}/clinical-records?clinicId={clinicId}` — veterinario vinculado:

```json
{
  "attentionDate": "2026-11-10T15:00:00Z",
  "reason": "Control general - ejemplo",
  "diagnosis": "Diagnóstico simulado",
  "treatmentSummary": "Seguimiento simulado",
  "observations": "No utilizar como información clínica real",
  "indicators": [
    { "name": "Peso", "value": 30.5, "unit": "kg" },
    { "name": "Frecuencia cardiaca", "value": 88, "unit": "lpm" }
  ]
}
```

Puedes incluir `&appointmentId={UUID_CITA}` en la URL **solo si la cita es compatible y todavía no tiene una atención vinculada**. De lo contrario puedes recibir un `409`.

`201` devuelve `attentionId`, `clinicalRecordId` y `petId`.

Para la evolución, registra **dos atenciones con el mismo indicador y unidad en fechas diferentes**, por ejemplo `Peso` en `kg`, y después consulta:

```http
GET /api/v1/pets/{petId}/evolution?clinicId={clinicId}
```

La respuesta contiene `hasSufficientData` y `entries`. Si aún no hay mediciones comparables, la lista puede estar vacía sin que falle el endpoint.

### 12.5. Citas: consultar disponibilidad, reservar y gestionar estados

Primero descubre opciones:

```http
GET /api/v1/clinics?page=1&pageSize=20
GET /api/v1/clinics/{clinicId}/veterinarians
GET /api/v1/appointments/availability?clinicId={clinicId}&veterinarianId={veterinarianId}&date=2026-11-10
```

El nuevo endpoint de disponibilidad devuelve `durationMinutes: 30` y `slots` (`startTime`, `endTime`). Considera horario de la clínica, agenda del veterinario y fechas pasadas. Las citas canceladas liberan sus franjas. **La consulta no reserva el horario**: al crear la cita, el backend debe volver a comprobar conflictos.

`POST /api/v1/appointments` — propietario:

```json
{
  "petId": "UUID_MASCOTA",
  "clinicId": "UUID_CLINICA",
  "veterinarianId": "UUID_VETERINARIO",
  "date": "2026-11-10",
  "startTime": "11:00:00",
  "reason": "Control preventivo - prueba"
}
```

La respuesta `201` contiene `appointmentId`, `status` (normalmente `Pending`), horario final y datos asociados.

Modificar la cita: **mismo PATCH, tres operaciones diferentes**.

`PATCH /api/v1/appointments/{appointmentId}` — reprogramar:

```json
{ "operation": "reschedule", "date": "2026-11-10", "startTime": "12:00:00" }
```

`PATCH /api/v1/appointments/{appointmentId}` — cancelar:

```json
{ "operation": "cancel" }
```

`PATCH /api/v1/appointments/{appointmentId}` — marcar atendida, después de registrar la atención clínica:

```json
{ "operation": "mark_attended", "clinicalAttentionId": "UUID_ATENCION_CLINICA" }
```

Otros GET:

```http
GET /api/v1/appointments/{appointmentId}
GET /api/v1/appointments/agenda?clinicId={clinicId}&from=2026-11-10&to=2026-11-17
```

La agenda es para `veterinarian`. No marques como atendida una cita sin una atención clínica compatible.

### 12.6. Medicación: prescripción, recordatorios y administración

`POST /api/v1/pets/{petId}/medication-plans` — veterinario:

```json
{
  "clinicId": "UUID_CLINICA",
  "clinicalAttentionId": "UUID_ATENCION_CLINICA",
  "medicationName": "Medicamento ficticio de prueba",
  "route": "Oral",
  "instructions": "Solo para pruebas de software. No suministrar a animales reales.",
  "startDate": "2026-11-10",
  "endDate": "2026-11-12",
  "timeZoneId": "America/Lima",
  "schedules": [
    {
      "localTime": "10:00:00",
      "daysOfWeek": ["Tuesday", "Wednesday", "Thursday"],
      "amount": 1,
      "unit": "unidad"
    }
  ]
}
```

**Atención:** ajusta fechas y días de semana a la fecha real de tu prueba. `clinicalAttentionId` es opcional en el contrato. El `POST` responde `201` con el `planId`.

Consultar el plan y las dosis:

```http
GET /api/v1/pets/{petId}/medication-plans
GET /api/v1/medication-plans/{planId}
GET /api/v1/medication-plans/{planId}/doses
GET /api/v1/medication-plans/{planId}/doses?from=2026-11-10&to=2026-11-12
```

La generación de dosis corre en un **worker**: inmediatamente después del `POST`, `/doses` podría devolver `[]` hasta que se ejecute el ciclo. En la configuración revisada, la generación está activada y su intervalo es de 30 minutos; también arranca al iniciar el servicio.

`PATCH /api/v1/medication-plans/{planId}` — cambiar horarios:

```json
{
  "operation": "changeSchedules",
  "schedules": [
    { "localTime": "11:00:00", "daysOfWeek": ["Tuesday", "Wednesday", "Thursday"], "amount": 1, "unit": "unidad" }
  ]
}
```

Alternativas del mismo PATCH:

```json
{ "operation": "complete" }
```

```json
{ "operation": "cancel" }
```

`PATCH /api/v1/medication-plans/{planId}/reminders` — propietario:

```json
{ "enabled": true }
```

**La activación del flag no prueba por sí sola que Firebase haya enviado una notificación real.**

`POST /api/v1/medication-doses/{doseId}/administrations` — propietario:

```json
{ "administeredAtUtc": "2026-11-10T16:05:00Z" }
```

Usa **el `doseId` de una dosis real generada y pendiente**, no el `planId` ni el `scheduleId`. La fecha debe ser coherente con la fecha actual. `201` devuelve `administrationId`; un GET posterior de dosis debe reflejar `Administered`. El flujo local probó una administración anticipada respecto al horario programado: revisa esa regla de negocio antes de producción.

`POST /api/v1/me/medication-devices` — propietario:

```json
{ "token": "TOKEN_FICTICIO_SOLO_PARA_PRUEBAS", "platform": "android" }
```

Admite plataformas `android`, `ios` y `web`. Un token ficticio sirve para probar persistencia, **no entrega push**.

`DELETE /api/v1/me/medication-devices/{deviceId}` responde normalmente `204`.

### 12.7. Alimentación: prescripción, revisión, recordatorios y desactivación

`POST /api/v1/pets/{petId}/feeding-plans` — veterinario:

```json
{
  "clinicId": "UUID_CLINICA",
  "foodName": "Alimento ficticio de prueba",
  "recommendations": "Datos simulados, no aptos como indicación veterinaria",
  "startDate": "2026-11-10",
  "endDate": "2026-11-12",
  "timeZoneId": "America/Lima",
  "schedules": [
    { "localTime": "08:00:00", "daysOfWeek": ["Tuesday", "Wednesday", "Thursday"], "quantity": 100, "unit": "g" },
    { "localTime": "18:00:00", "daysOfWeek": ["Tuesday", "Wednesday", "Thursday"], "quantity": 100, "unit": "g" }
  ]
}
```

`201` devuelve un `planId` y una versión inicial.

`PUT /api/v1/feeding-plans/{planId}` — veterinario, mismo esquema JSON, pero con nuevos datos u horarios; debe crear una versión nueva y conservar la anterior en `revisions`.

`PATCH /api/v1/feeding-plans/{planId}/reminders` — propietario:

```json
{ "enabled": true }
```

`PATCH /api/v1/feeding-plans/{planId}/deactivate` — veterinario, **sin cuerpo**. Desactiva el plan y los recordatorios, conservando el historial.

Consultas:

```http
GET /api/v1/feeding-plans/{planId}
GET /api/v1/pets/{petId}/feeding-plans/active
```

**Importante:** después de desactivar el plan, el GET `/active` debe devolver `404`; eso es esperado. La consulta por `planId` sigue existiendo y permite acceder al historial.

### 12.8. Constancia y gamificación

`GET /api/v1/me/adherence` — propietario, sin cuerpo ni parámetros.

Ejemplo de respuesta **antes de contar con evidencias procesadas**:

```json
{
  "ownerId": "UUID_OWNER",
  "hasSufficientEvidence": false,
  "evidenceCount": 0,
  "score": null,
  "level": null,
  "recognitions": []
}
```

Un `200` con cero evidencias comprueba la consulta, **no** el cálculo completo de constancia. Para verificarlo, registra actividades que produzcan evidencias y activa/valida los procesadores de eventos correspondientes en un entorno controlado.

## 13. Flujo completo de prueba local

Para probar VetPax en una **base de datos limpia**, sigue este orden; no intentes usar UUIDs de pruebas antiguas:

| Paso | Usuario | Acción | Qué guardar |
|---|---|---|---|
| 1 | Keycloak Admin | Verificar realm y crear roles/usuarios de pruebas. | UUIDs (`sub`) de admin y veterinario. |
| 2 | Público | `POST /auth/register`: crear propietario. | UUID del propietario. |
| 3 | Todos | `GET /auth/me` y `/permissions`. | Rol y permisos correctos. |
| 4 | Administrador clínica | `POST /clinics`. | `clinicId`. |
| 5 | Administrador clínica | `POST /clinics/{clinicId}/veterinarians`. | Confirmación `204`. |
| 6 | Propietario | `GET /clinics` y `/clinics/{clinicId}/veterinarians`. | Clínica y veterinario disponibles. |
| 7 | Propietario | `POST /pets`. | `petId`, `clinicalRecordId`. |
| 8 | Propietario | `GET /pets` y `PUT /pets/{petId}/clinics/{clinicId}`. | Mascota en clínica. |
| 9 | Veterinario | `GET /clinics/{clinicId}/patients`. | Mascota visible. |
| 10 | Propietario | `GET /appointments/availability` y `POST /appointments`. | `appointmentId`. |
| 11 | Veterinario | `GET /appointments/agenda`. | Cita en agenda. |
| 12 | Veterinario | `POST /pets/{petId}/clinical-records?clinicId=...&appointmentId=...`. | `attentionId`. |
| 13 | Veterinario | `PATCH /appointments/{appointmentId}` → `mark_attended`. | Estado `Attended`. |
| 14 | Veterinario | Registrar otra atención, con indicador comparable. | Segundo `attentionId`. |
| 15 | Veterinario | `GET /pets/{petId}/evolution?clinicId=...`. | `hasSufficientData: true`. |
| 16 | Veterinario | `POST /pets/{petId}/medication-plans`. | `planId`. |
| 17 | Propietario | `GET /medication-plans/{planId}/doses`, esperar generación. | `doseId` pendiente. |
| 18 | Propietario | `POST /medication-doses/{doseId}/administrations`. | `administrationId`. |
| 19 | Propietario | Repetir GET de dosis. | Estado `Administered`. |
| 20 | Veterinario | `POST /pets/{petId}/feeding-plans`. | `feedingPlanId`. |
| 21 | Veterinario/propietario | Actualizar plan, alternar recordatorios, consultar activo y desactivar. | Versiones y estados. |
| 22 | Propietario | `GET /me/adherence`. | Estado de evidencia (puede faltar worker). |

**Sugerencia:** guarda tus nuevos UUIDs en un archivo `.http` o colección de Postman privada, nunca en credenciales versionadas. Las cuentas de propietario, veterinario y administrador necesitan sesiones JWT distintas.

## 14. Workers y servicios en segundo plano

Los endpoints síncronos y el procesamiento automático son conceptos distintos. Por ejemplo, guardar `remindersEnabled: true` no equivale a enviar una notificación.

### 14.1. Configuración del código revisado

| Servicio | Sección / clave | Estado de referencia |
|---|---|---|
| Generación de dosis de medicación | `MedicationTreatment:Workers:DoseGenerationEnabled` | `true` por defecto. |
| Envío de recordatorios de medicación | `MedicationTreatment:Workers:RemindersEnabled` | `false` por defecto. |
| Outbox de medicación | `MedicationTreatment:Workers:OutboxEnabled` | `false` por defecto. |
| Procesamiento de nutrición | `NutritionManagement:Worker:Enabled` | `false` por defecto. |
| Generación de recordatorios de nutrición | `NutritionManagement:Worker:GenerationEnabled` | `false` por defecto. |
| Entrega de nutrición | `NutritionManagement:Worker:DeliveryEnabled` | `false` por defecto. |
| Outbox y recordatorios de citas | `AppointmentProcessing:Enabled` | `false` en `appsettings.json`. |
| Worker de reconocimientos | `AdherenceGamification:RecognitionWorker:Enabled` | `false` en Development. |
| Outbox de Adherence | `AdherenceGamification:OutboxWorker:Enabled` | `false` en Development. |

**Configuración ilustrativa para probar solo generación, sin envíos externos:** en `appsettings.Development.json` o User Secrets, ajusta únicamente los workers que realmente quieres comprobar. Ejemplo de objeto JSON parcial (no reemplaza todo el archivo):

```json
{
  "MedicationTreatment": {
    "Workers": {
      "DoseGenerationEnabled": true,
      "RemindersEnabled": false,
      "OutboxEnabled": false,
      "DoseGenerationIntervalMinutes": 30
    }
  },
  "NutritionManagement": {
    "Worker": {
      "Enabled": true,
      "GenerationEnabled": true,
      "DeliveryEnabled": false
    }
  }
}
```

Para Adherence, la política de referencia en `appsettings.Development.json` tiene `MinimumEvidence: 2`, umbrales Bronce 2, Plata 4, Oro 8, `AllowDemotion: false`. Estos son **umbrales de la configuración**, no compromisos clínicos validados.

**No habilites `DeliveryEnabled`, `RemindersEnabled` ni workers de envío push solamente para “ver si funciona”**: requieren credenciales y destinatarios adecuados, y pueden generar intentos de notificaciones externos.

### 14.2. Firebase Cloud Messaging

El proyecto Firebase que creaste anteriormente sirve para preparar el envío futuro de push. En el código revisado, Firebase Admin SDK busca:

- `Firebase:ProjectId` (en Development existe un valor de referencia `vetpax`; verifica que coincida con tu **Project ID real**).
- Credenciales de aplicación mediante **Application Default Credentials**; si usas JSON de servicio local, configurar `GOOGLE_APPLICATION_CREDENTIALS` a una ruta privada fuera de Git.

La aplicación móvil deberá solicitar permisos, obtener un **token FCM real** y registrar ese token mediante `POST /me/medication-devices`. Un token ficticio solo prueba la escritura en MySQL.

Se puede comprobar la persistencia, la lógica de generación y la preparación de mensajes sin aplicación móvil; la **recepción y apertura real en Android/iOS** debe probarse cuando exista el cliente.

## 15. Pruebas automatizadas

Desde la raíz:

```powershell
dotnet build .\VetPax-Backend.sln -c Debug
dotnet test .\VetPax-Backend.sln -c Debug
```

Para ejecutar solo el proyecto de pruebas:

```powershell
dotnet test .\tests\VetPax.Tests\VetPax.Tests.csproj -c Debug
```

El ZIP revisado contiene pruebas unitarias/de aplicación y algunos tests de integración marcados como `Skip`. Por eso, **un `dotnet test` verde no equivale a haber probado Keycloak, MySQL y Firebase de extremo a extremo**.

Las cuatro rutas nuevas fueron incorporadas y verificadas manualmente mediante Swagger en esta conversación; si no se han añadido pruebas automatizadas en tu repositorio actualizado, deben añadirse antes de la entrega final.

Pruebas adicionales recomendadas: usuarios de roles distintos (`401/403`), mascotas de otros propietarios, vínculo veterinario-clínica, conflictos de cita, administración de dosis duplicada, concurrencia, versiones de prescripciones y generación de recordatorios.

## 16. Errores frecuentes y soluciones

| Síntoma | Posible causa | Qué revisar |
|---|---|---|
| No aparece Swagger / la API no arranca | Secretos faltantes, Keycloak incorrecto, MySQL caído o errores de compilación. | Logs de la terminal, `dotnet build`, User Secrets, MySQL y `docker compose ps`. |
| `VetPax database connection is not configured` | Falta `ConnectionStrings:VetPaxDb`. | Configurar User Secrets o variable de entorno. |
| `Incomplete Keycloak configuration` | Falta `Keycloak:AdminClientSecret` u otro ajuste de Keycloak. | Confirmar cliente `vetpax-admin` y secreto de servidor. |
| `Connection refused` / `Unable to connect` | MySQL o Keycloak no está iniciado; puerto incorrecto. | `localhost:3306`, `localhost:8080`, logs Docker. |
| `No migrations were applied` | La base ya está actualizada. | Es informativo, no un error. |
| Swagger no autoriza / error de redirect URI | Cliente OAuth2 inexistente o callback no permitido. | `vetpax-swagger`, cliente público, PKCE S256 y `.../swagger/oauth2-redirect.html`. |
| `401 Unauthorized` | Token ausente, vencido, audiencia o issuer incorrectos. | Autorizar de nuevo; revisar realm `vetpax` y audiencia `vetpax-api`. |
| `403 Forbidden` | Rol incorrecto o recurso no autorizado. | `GET /auth/me`, `/permissions`, vínculo clínica/veterinario y propiedad de mascota. |
| `POST /clinics/{id}/veterinarians` devuelve 403 con `owner` | Estás intentando **vincular** un veterinario desde una cuenta de propietario. | Para listar usa **GET** con el mismo path; para vincular usa administrador. |
| `GET /feeding-plans/active` devuelve 404 | La mascota no tiene plan activo o ya fue desactivado. | Consultar por `planId` para ver historial. |
| `GET /medication-plans/{id}/doses` devuelve `[]` | El worker aún no generó dosis o el rango/plan no coincide. | Revisar logs de generación, fechas y configuración; no inventar `doseId`. |
| Error LINQ devuelto como `409` en nutrición | Puede ser una excepción de EF Core mapeada incorrectamente como conflicto. | Revisar la excepción interna del servidor, no asumir conflicto de negocio. |
| Cambiaste código pero sigue el comportamiento viejo | Estás usando `dotnet run --no-build`. | Ejecutar `dotnet build` y `dotnet run` **sin** `--no-build`. |
| Dos servicios intentan usar el mismo puerto | Docker o MySQL local ya ocupan `3306`/`8080`. | Detener el servicio duplicado o adaptar puerto y conexión. |
| No aparecen cuentas antiguas en Keycloak | Se levantó con un PostgreSQL nuevo o se perdió el volumen. | Revisar el volumen de Compose; usar respaldo/importación del realm y usuarios. |
| IDs de mascotas que funcionaban ahora dan 404 | Se instaló una MySQL vacía. | Las migraciones restauran el esquema, no los datos anteriores. |
| `Firebase:ProjectId is not configured` | Se intenta resolver el gateway FCM sin Project ID. | Configurar ID real y credenciales solo si corresponde habilitar push. |

### 16.1. Diagnóstico útil sin compartir secretos

Comparte con el equipo: **endpoint, método, estado HTTP, cuerpo de respuesta y stack trace del servidor sin secretos**. Evita enviar el `Authorization: Bearer ...`, contraseñas, secretos de cliente de Keycloak o archivos de service account de Firebase.

## 17. Buenas prácticas, respaldo y seguridad

### 17.1. Git y configuración

**No versionar:**

- `infra/keycloak/.env`.
- Claves reales de MySQL, Keycloak o Firebase.
- Tokens Bearer, `.pem`, JSON de cuentas de servicio o dumps con datos privados.
- Archivos de `bin/`, `obj/`, `.idea/` privados de la máquina (cuando corresponda).

**Sí versionar:** código, migraciones de EF Core, `.env.example` sin contraseñas reales, documentación, scripts de configuración que no contengan credenciales y tests.

> El export original de Keycloak y algunos archivos del ZIP pueden contener secretos o datos de desarrollo. Antes de subirlos a repositorios compartidos, revisa y elimina/rota cualquier secreto real. No es suficiente excluir nuevos secretos si un valor anterior ya fue commiteado.

### 17.2. Respaldo y restauración MySQL

**Respaldar tablas y datos** (ejemplo; solicitará contraseña):

```powershell
mysqldump -h 127.0.0.1 -P 3306 -u vetpax_app -p --single-transaction --routines --triggers vetpax_db > vetpax_db_backup.sql
```

El respaldo contiene **registros**, a diferencia de una migración. Guárdalo fuera del repositorio si incluye datos reales.

**Restaurar a otra base local** después de crearla y comprobar compatibilidad del esquema:

```powershell
mysql -h 127.0.0.1 -P 3306 -u vetpax_app -p vetpax_db < vetpax_db_backup.sql
```

> La redirección `<` puede no funcionar según la versión de PowerShell. Ejecuta estas órdenes desde **CMD** o un shell que admita la redirección, o utiliza la opción de importación de MySQL Workbench. **No combines un dump completo con migraciones iniciales sobre una base que ya tiene las mismas tablas**, porque puedes provocar duplicados o errores; elige restauración completa **o** base nueva con migraciones.

Para Keycloak, la **sección 5.5** documenta el respaldo completo de PostgreSQL con `pg_dump` y su restauración con `pg_restore` después de clonar GitHub. No basta respaldar MySQL.

### 17.3. Reglas antes de desplegar en internet

- No dejar clientes Keycloak con secretos accesibles al frontend.
- Configurar HTTPS público y orígenes CORS permitidos para la web.
- Cambiar contraseñas y secretos de desarrollo.
- Definir migraciones y respaldo para producción; en el código revisado las migraciones automáticas solo se aplican en `Development`.
- Probar acceso indebido entre propietarios y clínicas, validación, idempotencia y concurrencia de citas.
- Habilitar workers de forma controlada; comprobar reintentos y errores de notificaciones.
- No exponer Swagger ni páginas de administración sin controles adecuados en entornos productivos.

## 18. Estado y límites de la implementación

### 18.1. ¿Qué se ha comprobado manualmente?

Durante las pruebas locales de VetPax se verificaron los flujos principales de autenticación, roles, clínicas, mascotas, vínculos, citas, historia clínica y evolución, tratamientos, alimentación y registro de administración de medicamentos. También se probaron los cuatro GET añadidos para facilitar la integración de la app móvil:

- `GET /api/v1/pets`
- `GET /api/v1/clinics`
- `GET /api/v1/clinics/{clinicId}/veterinarians`
- `GET /api/v1/appointments/availability`

La consulta de disponibilidad devolvió intervalos de 30 minutos y liberó correctamente la franja de una cita cancelada. Las pruebas manuales son evidencia de los escenarios concretos ejecutados, **no garantizan todas las combinaciones de usuarios y datos**.

### 18.2. ¿Qué queda fuera del alcance de esta guía local?

- La **web** y la **app móvil** no están incluidas en el ZIP del backend y no son necesarias para iniciar la API local.
- La **entrega real de push FCM** requiere credenciales válidas, token real y un cliente móvil; preparar Firebase no significa que la entrega esté probada.
- **Adherence & Gamification** respondió inicialmente con cero evidencias; la ingestión automática, el cálculo real y los reconocimientos siguen requiriendo verificación con workers y eventos.
- Las integraciones contempladas en documentos de proyecto, como WebSockets clínicos, calendario externo o captación de leads, deben contrastarse por separado con el alcance final; no deben describirse como operativas si todavía no están implementadas.
- Los cambios más recientes de tu rama local pueden diferir del ZIP de referencia. **La fuente definitiva para los contratos es el código compilado y su OpenAPI local**, y este README debe actualizarse cuando cambien las rutas o modelos.

---

## Arranque rápido (recordatorio)

**Primera instalación:** si recibiste el respaldo de Keycloak del equipo, **antes de seguir estos comandos utiliza la [sección 5.5](#55-compartir-keycloak-con-el-equipo-respaldo-y-restauración-al-clonar)**. No arranques Keycloak sin restaurar cuando quieras conservar los usuarios existentes.

```powershell
# 1. Clonar repositorio (usar la URL real)
git clone URL_REAL_DEL_REPOSITORIO
cd VetPax-Backend

# 2. Keycloak (solo instalación nueva sin respaldo; con dump seguir la sección 5.5)
cd .\infra\keycloak
docker compose up -d
cd ..\..

# 3. Verificar MySQL 8 en localhost:3306 y crear vetpax_db
# 4. Configurar los dos secretos: ConnectionStrings:VetPaxDb y Keycloak:AdminClientSecret

# 5. Compilar y crear las tablas existentes
dotnet restore .\VetPax-Backend.sln
dotnet build .\VetPax-Backend.sln
dotnet ef database update --project .\VetPax.Infrastructure\VetPax.Infrastructure.csproj --startup-project .\VetPax.Api\VetPax.Api.csproj

# 6. Levantar la API
dotnet run --project .\VetPax.Api\VetPax.Api.csproj --launch-profile https
```

**Una vez configurado:** iniciar MySQL, `docker compose up -d` en `infra/keycloak` y `dotnet run` desde la raíz.

**Swagger local:** https://localhost:7175/swagger

---

**VetPax Backend · Guía de instalación y API para desarrollo local · 2026**
