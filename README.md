# NazarenoSonsonate.Mobile
📍 Nazareno Sonsonate Mobile
Aplicación móvil para el seguimiento en tiempo real de recorridos procesionales, desarrollada con .NET MAUI Blazor Hybrid.
Proporciona visualización geoespacial de rutas, monitoreo en vivo de unidades procesionales y herramientas de filtrado para mejorar la experiencia del usuario.
---
📌 Descripción
Nazareno Sonsonate Mobile permite:
Visualizar recorridos procesionales en un mapa interactivo.
Consultar información estructurada de los grupos participantes.
Seguir en tiempo real la ubicación de las andas procesionales.
Además:
Funciona tanto con conexión como en modo offline.
Mantiene datos almacenados localmente para mejorar rendimiento.
---
🚀 Características
📡 Tiempo real
Actualización en vivo de la ubicación de las andas.
Comunicación en tiempo real mediante SignalR.
Animación fluida del movimiento en el mapa.
Reconexión automática ante pérdida de conexión.
---
🗺️ Visualización geoespacial
Renderización de rutas con Google Maps.
Soporte para múltiples recorridos independientes.
Ajuste automático del mapa según el recorrido.
---
👥 Filtrado de datos
Filtrado por:
Cargadores
Cargadoras
Filtrado por número de grupo.
Persistencia del filtro seleccionado.
Las andas permanecen visibles al aplicar filtros.
---
📍 Gestión de ubicación
Visualización de la última ubicación registrada.
Persistencia local para carga rápida.
Validación de datos antes de enviarlos.
---
📶 Modo offline
Acceso a:
Recorridos
Puntos de ruta
Grupos
Última ubicación
Manejo de estado sin conexión.
Reconexión automática al recuperar internet.
---
🎯 Precisión de posicionamiento
Validación de precisión GPS antes de enviar.
Reintentos automáticos si la precisión es baja.
Prevención de ubicaciones incorrectas.
---
🎨 Experiencia de usuario
Globos personalizados en el mapa.
Eliminación de componentes visuales innecesarios de Google Maps.
Interfaz optimizada para dispositivos móviles.
---
🧱 Stack Tecnológico
📱 Mobile
.NET MAUI Blazor Hybrid
JavaScript (Google Maps API)
🌐 Backend
ASP.NET Core Web API
SignalR
Entity Framework Core
🗄️ Persistencia
SQL Server
---
🏗️ Arquitectura
Cliente móvil  
↓  
Servicio de ubicación (GPS)  
↓  
API (ASP.NET Core)  
↓  
Base de datos (SQL Server)  
↓  
SignalR Hub  
↓  
Cliente móvil (visualización)
---
⚙️ Componentes principales
`MapaRecorrido.razor` → Vista principal del mapa
`mapaRecorrido.js` → Lógica de renderización y animación
`SignalRService` → Comunicación en tiempo real
`UbicacionService` → Envío de ubicación
`UbicacionTrackingService` → Control de rastreo GPS
`PuntoRutaCacheService` → Gestión de cache local
---
📦 Instalación
```bash
git clone https://github.com/Andersson-Tepas/NazarenoSonsonate.Mobile.git
```
Requisitos
Visual Studio 2022 o superior
.NET 8 SDK
Dispositivo Android o emulador
Configuración
Configurar la URL del backend en `MauiProgram.cs`.
Compilar el proyecto.
Ejecutar en Android.
---
📊 Estado del proyecto
✔ Funcional
✔ Tiempo real operativo
✔ Manejo de conectividad
✔ Optimizado para móviles
---
📌 Versión
v1.1.0
Incluye:
Tiempo real completo
Filtros avanzados
Modo offline
Mejora de precisión GPS
Globos personalizados
---
👨‍💻 Autor
Andersson Tepas  
Técnico en Desarrollo de Software
---
📄 Licencia
Uso académico / demostrativo.
