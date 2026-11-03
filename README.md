# WMS Básico — .NET 8 + PostgreSQL

Aplicación inicial para administrar productos, categorías, bodegas, existencias y movimientos de entrada/salida.

## Requisitos

- Windows 10/11 o Windows Server.
- .NET 8 SDK para desarrollo; .NET 8 Hosting Bundle para publicar en IIS.
- PostgreSQL 15 o superior.
- Visual Studio 2022 o `dotnet` CLI (opcional).
- pgAdmin (opcional, para administrar PostgreSQL).

## 1. Crear la base de datos

1. Instala PostgreSQL desde https://www.postgresql.org/download/windows/
2. Abre pgAdmin o SQL Shell (psql).
3. Ejecuta `database/01_create_database.sql` conectado como administrador.
4. Cambia `CAMBIAR_PASSWORD` por una contraseña segura y usa exactamente la misma en la cadena de conexión.

Si creas usuario/base de datos manualmente, configura la cadena en `Wms.Api/appsettings.json`:

`Host=localhost;Port=5432;Database=wms_db;Username=wms_user;Password=TU_PASSWORD`

Para producción, es preferible definir `ConnectionStrings__WmsDb` como variable de entorno y no guardar secretos en el archivo.

## 2. Ejecutar localmente

Desde la carpeta donde está `Wms.sln`:

```powershell
dotnet restore
dotnet run --project .\Wms.Api
```

Abre la URL que indique la consola. La aplicación web es la ruta raíz `/`. En entorno Development, Swagger está disponible en `/swagger`.

En el primer arranque la API crea las tablas y carga dos categorías y una bodega de ejemplo. Esta versión usa `EnsureCreated` para facilitar el MVP. Antes de evolucionar el esquema en producción, se debe migrar a Entity Framework Core Migrations.

## 3. Publicar en IIS

### En el equipo de desarrollo

```powershell
dotnet publish .\Wms.Api\Wms.Api.csproj -c Release -o .\publish
```

Copia el contenido de `publish` al servidor, por ejemplo `C:\inetpub\wwwroot\WMS`.

### En el servidor Windows

1. Instala el **.NET 8 Hosting Bundle**: https://dotnet.microsoft.com/en-us/download/dotnet/8.0
2. Instala/configura PostgreSQL y crea la base de datos. Si PostgreSQL está en otro servidor, habilita conectividad solo desde el servidor IIS.
3. En IIS, crea un sitio web:
   - Physical path: `C:\inetpub\wwwroot\WMS`
   - Binding: HTTP/HTTPS según tu infraestructura.
   - Application Pool: `No Managed Code`.
4. Otorga al usuario del Application Pool permisos de lectura y ejecución sobre la carpeta publicada.
5. Configura la cadena de conexión como variable de entorno del servidor o del proceso:
   - Nombre: `ConnectionStrings__WmsDb`
   - Valor: `Host=SERVIDOR_POSTGRES;Port=5432;Database=wms_db;Username=wms_user;Password=TU_PASSWORD`
6. Reinicia el sitio y verifica `/api/health`.

Para el primer despliegue, asegúrate de que el usuario de PostgreSQL tenga permisos para crear tablas, índices y secuencias en `public`, porque el MVP inicializa el esquema al arrancar.

### Verificación

- `https://tu-dominio/` — interfaz web.
- `https://tu-dominio/api/health` — estado de la API.
- `/swagger` — solo se habilita por defecto en entorno Development. No habilitarlo públicamente en producción sin una decisión de seguridad.

## Funcionalidades incluidas

- Crear y consultar productos.
- Crear categorías.
- Crear bodegas.
- Consultar inventario por producto y bodega.
- Registrar entradas y salidas.
- Rechazar salidas si no hay existencias suficientes.
- Consultar los últimos 200 movimientos.

## Importante antes de usarlo con inventario real

Esta es una base MVP para desarrollo y validación funcional, no una solución empresarial endurecida. No incluye autenticación/autorización, auditoría de usuarios, concurrencia avanzada, backups automatizados, pruebas de carga ni migraciones versionadas. Antes de exponerlo en Internet o manejar inventario real, agregar autenticación, HTTPS, autorización por roles, validaciones y manejo de concurrencia, copias de seguridad y migraciones EF Core. Protege la cadena de conexión y no uses la contraseña de ejemplo.
