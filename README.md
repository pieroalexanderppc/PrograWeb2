# Concreta

Sistema web de apoyo al diseño de mezclas de concreto con cementos adicionados: calcula la dosificación por m³ (ACI 211.1), el costo y la huella de carbono, compara diseños, recomienda uno con una justificación y genera alternativas.

Proyecto del curso Programación Web II (Universidad Privada de Tacna, EPIS), alineado con el Reto 2 de la Hackathon Innova Sur 2026.

**Stack:** ASP.NET MVC 5 sobre .NET Framework 4.7.2 · Entity Framework 6 · SQL Server · Bootstrap 5.3.

---

## 1. Usuarios de prueba

Todos usan la contraseña **`Concreta123*`**.

| Correo | Rol | Qué puede hacer |
|---|---|---|
| `admin@concreta.pe` | Administrador | Todo, incluidos Materiales, Parámetros ACI y Usuarios |
| `cmamani@concreta.pe` | Ingeniero | Proyectos, diseños, comparaciones y alternativas |
| `lquispe@concreta.pe` | Laboratorista | Ver diseños y registrar ensayos |

> El script SQL crea los usuarios con la contraseña `HASH_PENDIENTE`. Al arrancar, la aplicación la reemplaza por el hash de `Concreta123*`. Si reinstalas la base con la aplicación abierta, **detenla y vuelve a ejecutarla**; si no, el login fallará.

---

## 2. Requisitos

- **Visual Studio 2022 o 2026** con la carga de trabajo **"Desarrollo de ASP.NET y web"** (incluye .NET Framework 4.7.2 e IIS Express).
- **SQL Server** 2019 o 2022, en edición Express o Developer (ambas son gratuitas).
- **SQL Server Management Studio (SSMS)** o `sqlcmd`, para ejecutar el script.
- **Internet** al ejecutar la app: Bootstrap, los íconos, jQuery y las fuentes se cargan desde CDN.

---

## 3. Instalar la base de datos

El script `database/ConcretaDB.sql` crea la base **ConcretaDB** con sus 13 tablas, el procedimiento almacenado, la vista y los datos de ejemplo (materiales, tablas ACI, un proyecto y dos diseños).

**Opción A, con SSMS:**
1. Conéctate a tu servidor.
2. Abre `database/ConcretaDB.sql`.
3. Presiona **Ejecutar** (F5).

**Opción B, con la consola:**

```powershell
sqlcmd -S localhost -E -f 65001 -i database\ConcretaDB.sql
```

- `-S` es el nombre de tu servidor (mira la sección 4).
- `-E` usa la autenticación de Windows.
- `-f 65001` lee el archivo como UTF-8 para que las tildes se guarden bien.

**Para reinstalar desde cero**, borra la base y vuelve a ejecutar el script:

```powershell
sqlcmd -S localhost -E -Q "DROP DATABASE ConcretaDB"
```

**Para comprobar que quedó bien:**

```powershell
sqlcmd -S localhost -E -d ConcretaDB -Q "SELECT Codigo, CostoTotalM3, HuellaCO2M3 FROM DisenoMezcla"
```

Debe mostrar `DM-2026-0001` (339.85 / 341.66) y `DM-2026-0002` (323.73 / 242.41).

---

## 4. Qué cambiar si lo instalas en otra PC

### 4.1 La cadena de conexión (lo único obligatorio)

Está en **`Concreta/Web.config`**:

```xml
<connectionStrings>
  <add name="ConcretaDB"
       connectionString="Data Source=localhost;Initial Catalog=ConcretaDB;Integrated Security=True;MultipleActiveResultSets=True"
       providerName="System.Data.SqlClient" />
</connectionStrings>
```

Cambia **`Data Source`** según cómo esté instalado SQL Server en esa PC:

| Instalación | `Data Source` | `-S` en sqlcmd |
|---|---|---|
| Developer o instancia por defecto | `localhost` | `localhost` |
| **SQL Server Express** (lo más común) | `localhost\SQLEXPRESS` | `localhost\SQLEXPRESS` |
| LocalDB (viene con Visual Studio) | `(localdb)\MSSQLLocalDB` | `"(localdb)\MSSQLLocalDB"` |

**¿Cómo saber cuál tengo?** En SSMS, el nombre aparece en la ventana de conexión. También lo puedes ver en PowerShell:

```powershell
Get-Service MSSQL*      # MSSQLSERVER = localhost · MSSQL$SQLEXPRESS = localhost\SQLEXPRESS
```

**Si usas usuario y contraseña de SQL** en lugar de la autenticación de Windows, reemplaza `Integrated Security=True` por:

```
User ID=sa;Password=tu_contraseña;TrustServerCertificate=True
```

No subas al repositorio un `Web.config` con contraseñas reales.

### 4.2 Otros detalles

- **Paquetes NuGet:** si al abrir la solución faltan paquetes, haz clic derecho en la solución y elige **Restaurar paquetes NuGet**. También puedes ejecutar `Update-Package -reinstall` en la Consola del Administrador de paquetes.
- **Puerto:** la app usa `http://localhost:59556/`. Si ese puerto está ocupado, cámbialo en las propiedades del proyecto, pestaña **Web**.
- **No hace falta cambiar nada más.** La cultura (es-PE), la autenticación y la codificación ya están configuradas en `Web.config`.

---

## 5. Ejecutar

1. Abre **`Concreta.slnx`** en Visual Studio.
2. Verifica que el proyecto de inicio sea **Concreta** (no `Concreta.Tests`): clic derecho en el proyecto **Concreta** → **Establecer como proyecto de inicio**.
3. Presiona **F5**. Se abre el login.
4. Entra con uno de los usuarios de la sección 1.

**Pruebas unitarias:** abre el **Explorador de pruebas** y pulsa "Ejecutar todas", o usa la consola después de compilar:

```powershell
dotnet test Concreta.Tests\bin\Debug\net472\Concreta.Tests.dll
```

---

## 6. Estructura

```
Concreta/                          ← raíz del repositorio
├─ README.md                       ← este archivo
├─ CLAUDE.md                       ← reglas del proyecto y lógica de dominio (fórmulas ACI, puntaje)
├─ DESIGN.md                       ← sistema de diseño (colores, tipografía, componentes)
├─ Concreta.slnx                   ← solución de Visual Studio
├─ database/
│  └─ ConcretaDB.sql               ← crea la base, tablas, procedimiento, vista y datos de ejemplo
├─ packages/                       ← paquetes NuGet
├─ Concreta/                       ← aplicación web (ASP.NET MVC 5)
│  ├─ App_Start/                   ← rutas y filtros globales
│  ├─ Controllers/                 ← un controlador por módulo (Disenos, Comparaciones, Alternativas, …)
│  ├─ Data/                        ← ConcretaDbContext (EF6) y DbSeeder (contraseñas iniciales)
│  ├─ Helpers/                     ← formato de números es-PE y ayudas para las vistas
│  ├─ Models/Entities/             ← clases que representan las tablas
│  ├─ Seguridad/                   ← login con cookie, roles y atributo [Autorizar]
│  ├─ Services/                    ← lógica de dominio:
│  │    ├─ DisenoAciService           cálculo de la dosificación ACI 211.1
│  │    ├─ CostoCO2Service            costo (S/ por m³) y huella (kg CO₂e por m³)
│  │    ├─ RecomendacionService       puntaje y justificación de la comparación
│  │    └─ GeneradorAlternativasService  optimizador: una variante por cemento, con y sin aditivo
│  ├─ ViewModels/                  ← modelos de los formularios, con validaciones en español
│  ├─ Views/                       ← vistas Razor (Shared/ tiene el layout y los componentes)
│  ├─ Content/site.css             ← estilos según DESIGN.md
│  ├─ Scripts/concreta.js          ← interacciones mínimas (toasts, modales, campos condicionales)
│  └─ Web.config                   ← cadena de conexión y configuración
└─ Concreta.Tests/                 ← pruebas unitarias (MSTest) de los servicios
```

### Base de datos

| Grupo | Tablas |
|---|---|
| Seguridad | `Rol`, `Usuario` |
| Materiales | `TipoMaterial`, `Material` |
| Parámetros ACI 211.1 | `ParametroAguaAire`, `ParametroRelacionAC`, `ParametroAgregadoGrueso` |
| Diseño | `Proyecto`, `DisenoMezcla`, `DetalleMezcla`, `EnsayoLaboratorio` |
| Comparación | `Comparacion`, `ComparacionDetalle` |

Además incluye el procedimiento `sp_CalcularTotalesDiseno` y la vista `vw_ResumenDiseno`.

---

## 7. Problemas frecuentes

| Síntoma | Causa y solución |
|---|---|
| *"A network-related or instance-specific error…"* al abrir la app | El `Data Source` no coincide con tu servidor (sección 4.1), o el servicio de SQL Server está detenido. |
| *"Cannot open database ConcretaDB"* | Falta ejecutar el script (sección 3). |
| El login dice que la contraseña no es correcta | Se reinstaló la base con la app abierta. Detén la app y vuelve a ejecutarla. |
| Las tildes salen como "DiseÃ±os" | Un archivo se guardó sin BOM. Guárdalo con *Archivo → Guardar con codificación → UTF-8 con firma*. |
| La página sale sin estilos | No hay internet: los estilos se cargan desde CDN. |
