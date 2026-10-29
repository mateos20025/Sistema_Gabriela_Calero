# Sistema Consultorio – Dra. Gabriela Calero

Sistema web de citas para un consultorio médico en Ecuador. Blazor Web App (.NET 10, interactividad Server) + MudBlazor + PostgreSQL 17, con arquitectura limpia en un monolito modular.

## Contenido

1. [Qué hace hoy el sistema](#qué-hace-hoy-el-sistema)
2. [Requisitos](#requisitos)
3. [Instalación en una computadora nueva (una sola vez)](#instalación-en-una-computadora-nueva-una-sola-vez)
4. [Uso diario](#uso-diario)
5. [Primeros pasos dentro del sistema](#primeros-pasos-dentro-del-sistema)
6. [Pruebas automáticas](#pruebas-automáticas)
7. [Comandos frecuentes](#comandos-frecuentes)
8. [Problemas frecuentes](#problemas-frecuentes)
9. [Configuración](#configuración)
10. [Estructura del proyecto](#estructura-del-proyecto)

Todos los comandos se escriben en **Developer PowerShell** (o en el Símbolo del sistema), **uno por uno**, dentro de la carpeta del proyecto.

---

## Qué hace hoy el sistema

| Módulo | Estado | Qué incluye |
|---|---|---|
| Login y usuarios | Listo | Inicio de sesión, roles Doctora y Asistente, contraseña temporal obligatoria de cambiar, bloqueo tras 5 intentos fallidos, gestión de usuarios (solo la doctora). |
| Pacientes | Listo | Registro por cédula ecuatoriana con consentimiento de datos, búsqueda, edición, deshabilitar y habilitar, historial de cambios (solo la doctora). |
| Pagos | Listo | Transferencias con captura (validar o rechazar), efectivo en el consultorio, anulación, detección de capturas repetidas, cuenta bancaria y precio sugerido (los define la doctora). Un pago puede asociarse a una cita. |
| Citas y agenda | Listo | Solicitud de cita en línea (con captcha y consentimiento), cola de llamadas, confirmación, reprogramación, cancelación, agenda diaria y semanal, horario de atención y bloqueos (los define la doctora), historial de cada cita. |
| Sitio público y diseño | Listo | Página de inicio con la identidad de la doctora (turquesa y rosa, tipografía Poppins, fotos reales): servicios, sobre mí, cómo agendar, consultorios, horario real de la agenda y botón de WhatsApp. El panel usa los mismos colores. |
| WhatsApp, Historia clínica | Pendiente | |

Direcciones (con la app corriendo):

| Página | Dirección |
|---|---|
| Página pública | https://localhost:7111/ |
| Solicitar cita (pública) | https://localhost:7111/citas/solicitar |
| Política de privacidad (pública) | https://localhost:7111/privacidad |
| Panel del personal | https://localhost:7111/admin |
| Agenda | https://localhost:7111/admin/agenda |
| Cola de citas | https://localhost:7111/admin/citas |
| Configuración de la agenda (solo doctora) | https://localhost:7111/admin/agenda/configuracion |
| Pacientes | https://localhost:7111/admin/pacientes |
| Pagos | https://localhost:7111/admin/pagos |
| Configuración de pagos (solo doctora) | https://localhost:7111/admin/pagos/configuracion |
| Usuarios (solo doctora) | https://localhost:7111/admin/usuarios |

---

## Requisitos

Instala estos programas una sola vez y comprueba que responden.

**SDK de .NET 10**: descárgalo desde https://dotnet.microsoft.com/download/dotnet/10.0

```powershell
dotnet --version
```

Deberías ver un número que empieza con `10.`

**Docker Desktop**: descárgalo desde https://www.docker.com/products/docker-desktop/ y ábrelo.

```powershell
docker --version
```

Deberías ver algo como `Docker version 29.x`.

**Git**:

```powershell
git --version
```

---

## Instalación en una computadora nueva (una sola vez)

### Paso 1. Descargar el proyecto

```powershell
git clone https://github.com/mateos20025/Sistema_Gabriela_Calero.git
```

```powershell
cd Sistema_Gabriela_Calero
```

### Paso 2. Instalar las herramientas del proyecto

Instala `dotnet-ef` (la herramienta de migraciones) en la versión exacta que usa el proyecto.

```powershell
dotnet tool restore
```

Deberías ver: `Restauración realizada correctamente.`

### Paso 3. Crear el archivo `.env`

El archivo `.env` guarda la contraseña de la base de datos. **Nunca se sube a GitHub** (ya está en `.gitignore`).

```powershell
copy .env.example .env
```

```powershell
notepad .env
```

En el Bloc de notas cambia `POSTGRES_PASSWORD` por una contraseña larga y aleatoria (por ejemplo, 30 letras y números) y guarda. Anótala: la necesitas en el paso 5.

### Paso 4. Encender la base de datos

Abre **Docker Desktop** y espera a que diga que está corriendo. Luego:

```powershell
docker compose up -d
```

Deberías ver: `Container consultorio-db Started`. Para comprobar que está lista:

```powershell
docker compose ps
```

En la columna `STATUS` debe decir `Up ... (healthy)`.

### Paso 5. Configurar los secretos de la aplicación

Los secretos se guardan en tu usuario de Windows (User Secrets), fuera del proyecto. Cambia `LA_CONTRASEÑA_DEL_ENV` por la contraseña del paso 3.

```powershell
dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Port=5432;Database=consultorio;Username=consultorio;Password=LA_CONTRASEÑA_DEL_ENV" --project src/Consultorio.Web
```

Datos de la primera cuenta de la doctora. La contraseña debe tener **al menos 10 caracteres, una mayúscula, una minúscula y un número**. Es temporal: el sistema obligará a cambiarla en el primer ingreso.

```powershell
dotnet user-secrets set "UsuarioInicial:NombreCompleto" "Gabriela Calero" --project src/Consultorio.Web
```

```powershell
dotnet user-secrets set "UsuarioInicial:Email" "correo-de-la-doctora@ejemplo.com" --project src/Consultorio.Web
```

```powershell
dotnet user-secrets set "UsuarioInicial:Password" "ClaveTemporal2026" --project src/Consultorio.Web
```

Para revisar lo que quedó guardado:

```powershell
dotnet user-secrets list --project src/Consultorio.Web
```

### Paso 6. Confiar en el certificado HTTPS de desarrollo

```powershell
dotnet dev-certs https --trust
```

Si aparece una ventana de Windows preguntando si confías en el certificado, responde **Sí**. Deberías ver `Successfully trusted`.

### Paso 7. Ejecutar la aplicación

```powershell
dotnet run --project src/Consultorio.Web
```

La primera vez tarda un poco porque compila. Al arrancar, la aplicación crea las tablas, los roles Doctora y Asistente y la cuenta inicial de la doctora. Deberías ver:

```
Cuenta inicial de la doctora creada (...). Deberá cambiar la contraseña en su primer inicio de sesión.
Now listening on: https://localhost:7111
```

Deja esa ventana abierta: si la cierras, la aplicación se apaga. Para detenerla, presiona `Ctrl + C`.

En la primera ejecución también verás dos líneas `ERR ... "__EFMigrationsHistory"`. Son normales: EF Core busca su tabla de control antes de crearla. No vuelven a aparecer.

También puedes abrir `Consultorio.slnx` en Visual Studio y presionar `F5` con el perfil `https`.

### Paso 8. Entrar al sistema

Abre https://localhost:7111/admin, inicia sesión con el correo y la contraseña del paso 5, y crea tu contraseña propia cuando el sistema lo pida.

---

## Uso diario

Una vez instalado, cada día solo necesitas:

1. Abrir **Docker Desktop**.
2. Encender la base de datos (si ya está encendida, no pasa nada):

   ```powershell
   docker compose up -d
   ```

3. Ejecutar la aplicación:

   ```powershell
   dotnet run --project src/Consultorio.Web
   ```

4. Abrir https://localhost:7111/admin

Para terminar: `Ctrl + C` en la ventana de la aplicación. Si además quieres apagar la base de datos (los datos se conservan):

```powershell
docker compose stop
```

Cuando descargues cambios nuevos del repositorio (`git pull`), no hace falta nada más: al arrancar, la aplicación aplica sola las migraciones pendientes.

---

## Primeros pasos dentro del sistema

1. **Doctora**: en **Usuarios**, pulsa **Nuevo usuario** para crear la cuenta de la asistente. El sistema genera una contraseña temporal que se muestra **una sola vez**: entrégala en persona.
2. **Asistente**: inicia sesión con esa contraseña temporal y crea la suya.
3. **Pacientes**: en **Pacientes**, pulsa **Nuevo paciente**, completa los datos y marca la casilla de consentimiento (es obligatoria por la LOPDP).
4. **Doctora**: en **Agenda → Configuración**, define la duración de las citas, la anticipación mínima y máxima, el horario de atención de cada día (franjas) y, si quieres recibir solicitudes en línea, activa **Los pacientes pueden pedir cita desde la página web**. Ahí mismo se bloquean vacaciones o feriados.
5. **Doctora**: en **Pagos → Configuración**, registra el precio sugerido de la consulta y la cuenta bancaria para transferencias.
6. **Citas**: el ciclo de una cita es:
   - **Desde la web**: el paciente elige día y hora en `/citas/solicitar`, deja sus datos y acepta la política de privacidad. La solicitud queda **Pendiente** y el horario reservado por las horas configuradas (nunca más allá de la hora de la cita). Si nadie la atiende a tiempo, el sistema la cancela solo y libera el horario.
   - **En Citas** (cola de trabajo): **Nuevas** → **Pasar a la cola de llamadas** → llamar al paciente → **Confirmar** o **No contestó** (se puede volver a intentar). Si la cédula ya existía, la ficha del paciente **no** cambia: lo que escribió en la web se muestra aparte para compararlo al llamar.
   - **Desde el panel**: en **Agenda**, pulsa **Agendar** en un horario libre (o **Agendar cita**), también desde la ficha del paciente. El horario queda reservado y la cita entra en **Citas → Por llamar** (estado "En cola"); se pulsa **Confirmar** después de hablar con el paciente.
   - En la ficha de la cita: **Reprogramar** (elige otro horario libre; queda Reprogramada hasta que el paciente confirme), **Cancelar** (con motivo) y, desde la hora de la cita, **Marcar atendida** o **Marcar no asistió**.
7. **Pagos**: en **Pagos**, desde la ficha del paciente o desde la ficha de la cita, pulsa **Registrar pago**.
   - **Transferencia**: adjunta la captura que envió el paciente (JPG, PNG o PDF, máximo 5 MB). Queda "Por validar" hasta que alguien confirme que el dinero llegó a la cuenta y pulse **Validar** (o **Rechazar**, con motivo).
   - **Efectivo**: queda "Por cobrar" hasta que el paciente pague en el consultorio y se pulse **Marcar como pagado**.
   - Un pago pendiente registrado por error se **anula** (con motivo); nunca se borra.

Qué puede hacer cada rol:

| Acción | Doctora | Asistente |
|---|---|---|
| Registrar, buscar y editar pacientes | Sí | Sí |
| Deshabilitar y habilitar pacientes | Sí | Sí |
| Registrar pagos, validar o rechazar transferencias, cobrar efectivo, anular | Sí | Sí |
| Agendar, confirmar, reprogramar y cancelar citas; atender la cola | Sí | Sí |
| Configurar la agenda (duración, horario de atención, bloqueos, solicitudes web) | Sí | No |
| Ver la cuenta bancaria (para enviarla a los pacientes) | Sí | Sí |
| Cambiar el precio sugerido y la cuenta bancaria | Sí | No |
| Ver el historial de cambios de pacientes y pagos | Sí | No |
| Crear usuarios, cambiar roles, restablecer contraseñas | Sí | No |

Un paciente **deshabilitado** no aparece en las búsquedas, conserva todos sus datos y no se puede editar, recibir pagos ni agendar citas hasta habilitarlo. Para verlo, activa **Mostrar deshabilitados** en la lista.

Sobre los montos: se pueden escribir como `35`, `35,50` o `35.50`. Un monto ambiguo como `1.234` se rechaza; escríbelo como `1234` o `1.234,00`.

---

## Pruebas automáticas

```powershell
dotnet test
```

Deberías ver dos líneas `Correctas!` (Domain y Application) con `Con error: 0`.

---

## Comandos frecuentes

**Crear una migración** después de cambiar entidades o configuraciones de EF Core:

```powershell
dotnet ef migrations add NombreDeLaMigracion --project src/Consultorio.Infrastructure --startup-project src/Consultorio.Web --output-dir Persistencia/Migraciones
```

**Aplicar migraciones sin arrancar la app**:

```powershell
dotnet ef database update --project src/Consultorio.Infrastructure --startup-project src/Consultorio.Web
```

**Restablecer una contraseña sin entrar al panel** (emergencia, por ejemplo si la doctora olvidó la suya). Genera una contraseña temporal, desbloquea y reactiva la cuenta:

```powershell
dotnet run --project src/Consultorio.Web -- restablecer-password correo@ejemplo.com
```

La contraseña aparece en la consola, debajo de la línea `WRN ... restablecida desde la consola del servidor`. Cada ejecución genera una nueva y anula la anterior.

**Ver las últimas 50 líneas del log más reciente** (en PowerShell; los logs se guardan 30 días en `src/Consultorio.Web/logs/`):

```powershell
Get-ChildItem src/Consultorio.Web/logs | Sort-Object LastWriteTime | Select-Object -Last 1 | Get-Content -Tail 50 -Encoding UTF8
```

**Entrar a la base de datos** con la consola de PostgreSQL (para salir escribe `\q`):

```powershell
docker exec -it consultorio-db psql -U consultorio -d consultorio
```

---

## Problemas frecuentes

| Mensaje o síntoma | Causa | Solución |
|---|---|---|
| `failed to connect to the docker API` | Docker Desktop está cerrado. | Abre Docker Desktop, espera a que arranque y repite `docker compose up -d`. |
| `Failed to connect to 127.0.0.1:5432` | La base de datos no está encendida. | `docker compose up -d` |
| `Define POSTGRES_PASSWORD en el archivo .env` | Falta el archivo `.env`. | Repite el paso 3 de la instalación. |
| `password authentication failed for user "consultorio"` | La contraseña de User Secrets no coincide con la del `.env`. | Repite el paso 5 con la contraseña correcta. Si cambiaste la del `.env` después de crear la base, la base conserva la anterior. |
| `Falta la cadena de conexión 'ConnectionStrings:Default'` | No se configuraron los User Secrets. | Repite el paso 5. |
| `No se pudo crear la cuenta inicial de la doctora: ...` | La contraseña inicial no cumple la política. | Usa 10 o más caracteres con mayúscula, minúscula y número (paso 5). |
| `Configura 'ProteccionDatos:VersionPoliticaPrivacidad'` | Falta esa sección en la configuración. | Revisa `src/Consultorio.Web/appsettings.json`. |
| El navegador dice "La conexión no es privada" | El certificado de desarrollo no es de confianza. | Repite el paso 6 y reinicia el navegador. |
| `address already in use` o el puerto 5432 está ocupado | Hay otro PostgreSQL instalado en la computadora. | Detén el otro servicio de PostgreSQL o cambia el puerto en `docker-compose.yml` y en la cadena de conexión. |
| "Cuenta bloqueada por demasiados intentos" | 5 contraseñas incorrectas seguidas. | Espera 15 minutos, pide a la doctora que la desbloquee en Usuarios, o usa el comando de emergencia. |
| "Demasiados intentos. Espera un minuto" | Más de 10 envíos de inicio de sesión por minuto, o más de 5 solicitudes de cita en 10 minutos, desde la misma conexión. | Espera unos minutos. |
| La página pública dice "Por ahora no recibimos solicitudes de cita en línea" | La agenda no está configurada, la doctora no activó las solicitudes web, falta el texto de la política de privacidad o (fuera de desarrollo) faltan las claves de Turnstile. | Revisa **Agenda → Configuración**, `ProteccionDatos:RutaPoliticaPrivacidad` y la sección `Turnstile`. |
| "La agenda aún no está configurada" | La doctora todavía no guardó la configuración de la agenda. | **Agenda → Configuración**. |
| "Hay N cita(s) activa(s) en ese período" al bloquear | No se bloquea encima de citas activas. | Reprográmalas o cancélalas y vuelve a bloquear. |
| "La captura debe ser una imagen JPG o PNG, o un PDF" | El archivo no es realmente una imagen o un PDF (aunque su nombre termine en .png). | Pide al paciente una captura de pantalla o el PDF que entrega el banco. |
| "Esta captura ya se registró el ..." | Esa misma captura ya está en otro pago vigente. | Revisa el pago indicado; si fue un error, anúlalo y vuelve a registrar. |
| `Configura 'Archivos:RutaBase'` | Falta la carpeta donde se guardan las capturas. | Revisa `src/Consultorio.Web/appsettings.json`. |
| `SitioPublico: ...` al arrancar | Falta un dato o hay un error en `Contenido/sitio-publico.json` (por ejemplo, una coma de más). | Corrige lo que indica el mensaje; si el error es de formato, revisa comas y comillas del archivo. |

---

## Configuración

Se define en `src/Consultorio.Web/appsettings.json`; en producción se puede sobrescribir con variables de entorno (por ejemplo `Consultorio__Nombre`).

| Clave | Para qué |
|---|---|
| `Consultorio:Nombre` y `Consultorio:NombreCorto` | Nombre que se muestra en la web, en el panel y en los títulos de las pestañas. |
| `ProteccionDatos:VersionPoliticaPrivacidad` | Versión de la política de privacidad que acepta cada paciente. Súbela cuando cambie el texto de la política. |
| `BaseDatos:AplicarMigracionesAlIniciar` | Aplica las migraciones pendientes al arrancar (válido con una sola instancia de la app). |
| `UsuarioInicial:*` | Cuenta inicial de la doctora; solo se usa si todavía no existe ninguna. **Nunca en el repositorio.** |
| `ConnectionStrings:Default` | Conexión a PostgreSQL. **Nunca en el repositorio.** |
| `DataProtection:RutaClaves` | Carpeta donde se guardan las claves que cifran las sesiones (necesaria en producción con Docker). |
| `ProteccionDatos:RutaPoliticaPrivacidad` | Archivo de texto con la política de privacidad que se muestra en `/privacidad` (relativo a `src/Consultorio.Web` o ruta completa). En desarrollo apunta a la plantilla `Contenido/politica-privacidad.plantilla.txt`. Sin este archivo no se reciben solicitudes en línea. Formato: `## ` para títulos, `- ` para listas, línea en blanco entre párrafos. |
| `Turnstile:ClaveSitio` y `Turnstile:ClaveSecreta` | Captcha de Cloudflare Turnstile del formulario público. En desarrollo se usan las claves de prueba públicas de Cloudflare (siempre aprueban). En producción, la clave secreta va **solo** en la variable de entorno `Turnstile__ClaveSecreta`. Fuera de desarrollo, sin claves no se reciben solicitudes en línea. |
| `SitioPublico:*` (archivo `src/Consultorio.Web/Contenido/sitio-publico.json`) | Contenido de la página de inicio: lema, especialidades, presentación, biografía, fotos, WhatsApp, redes sociales, pilares, servicios y consultorios. Se edita ese archivo y el cambio se ve al recargar la página, sin reiniciar. Si un dato obligatorio falta o un enlace no es `https`, la app no arranca y el mensaje dice qué corregir. El horario de atención no se escribe aquí: sale de **Agenda → Configuración**. |
| `Archivos:RutaBase` | Carpeta de las capturas de pago (por defecto `almacen`, dentro de `src/Consultorio.Web`). Está **fuera de `wwwroot`**: solo se pueden ver con sesión iniciada. Contiene datos personales: no se sube a GitHub y debe incluirse en los respaldos. |

Si falta una clave obligatoria, la aplicación no arranca y el mensaje indica cuál es.

**Contenido de la página de inicio** (`Contenido/sitio-publico.json`):

- **Fotos**: van en `src/Consultorio.Web/wwwroot/img/sitio/` (JPG de unos 900 px de ancho y menos de 200 KB) y se indican con su ruta, por ejemplo `img/sitio/doctora-principal.jpg`. Las actuales salen de la sesión fotográfica de la doctora; no se usan fotos con menores.
- **Íconos de servicios y pilares** (campo `Icono`): `ginecologia`, `embarazo`, `colposcopia`, `papanicolaou`, `anticoncepcion`, `fertilidad`, `rejuvenecimiento`, `paquete`, `prevenir`, `escuchar`, `acompanar`, `bienestar`.
- **Logotipo**: `Logo` es el emblema cuadrado (luna y silueta) que se ve en el encabezado, el pie y el panel; `LogoCompleto` es el logo con el nombre, que se ve en el inicio de sesión. Ambos son PNG con fondo transparente en `wwwroot/img/sitio/`. El ícono de la pestaña del navegador está en `wwwroot/favicon.png` y `wwwroot/apple-touch-icon.png`. El archivo original del logo está en la carpeta `Material` (fuera del repositorio).
- **Consultorios**: `UrlMapa` es opcional; si está vacío, el botón "Cómo llegar" busca el lugar y la dirección en Google Maps.
- Los textos de biografía y servicios se redactaron a partir de su Instagram y sus flyers: **la doctora debe revisarlos** antes de publicar.

**Antes de abrir las solicitudes en línea al público** (lo debe proporcionar la doctora):

1. Las claves reales de Cloudflare Turnstile (cuenta gratuita en https://dash.cloudflare.com, sección Turnstile, con el dominio del consultorio).
2. El texto final de la política de privacidad, revisado a partir de la plantilla, y la versión correspondiente en `ProteccionDatos:VersionPoliticaPrivacidad`.
3. La configuración de la agenda (duración, horario de atención, reserva y límites) desde el panel.

Al publicar detrás de Caddy, habrá que configurar las cabeceras reenviadas (`X-Forwarded-For`) para que el límite de envíos se aplique a la IP real de cada visitante y no a la del proxy; se hará en el módulo de despliegue.

---

## Estructura del proyecto

```
Consultorio.slnx
docker-compose.yml               Base de datos PostgreSQL para desarrollo
.env.example                     Plantilla del archivo .env (sin contraseñas reales)
dotnet-tools.json                Herramientas del proyecto (dotnet-ef)
Directory.Build.props            Opciones comunes de compilación
Directory.Packages.props         Versiones de los paquetes NuGet
src/
  Consultorio.Domain/            Entidades y reglas de negocio (sin dependencias)
  Consultorio.Application/       Casos de uso, puertos (interfaces) y validadores
  Consultorio.Infrastructure/    EF Core + PostgreSQL, Identity, auditoría, migraciones
  Consultorio.Web/               Blazor: página pública, /citas/solicitar, /cuenta y panel /admin
    Contenido/                   Contenido del sitio (sitio-publico.json) y plantilla de la política de privacidad
    Components/Sitio/            Encabezado, pie, emblema y botón de WhatsApp del sitio público
    wwwroot/css/sitio.css        Estilos y colores de la marca del sitio público
    wwwroot/img/sitio/           Fotos del sitio
    wwwroot/fonts/poppins/       Tipografía Poppins (licencia OFL)
tests/
  Consultorio.Domain.Tests/      Reglas del dominio (cédula, teléfono, paciente, pagos, citas y disponibilidad)
  Consultorio.Application.Tests/ Casos de uso y validadores
```
