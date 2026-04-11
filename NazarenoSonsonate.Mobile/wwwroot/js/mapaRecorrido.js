window.mapaRecorrido = {
    map: null,
    dataLayer: null,
    marcadorTiempoReal: null,
    marcadorUsuario: null,
    infoTiempoReal: null,
    puntoMarkers: [],
    todosLosPuntos: [],
    filtroActual: "ninguno",
    grupoSeleccionado: null,
    tipoSeleccionado: null,

    esperarGoogleMaps: async function () {
        let intentos = 0;

        while ((!window.googleMapsReady || !window.google || !google.maps) && intentos < 50) {
            await new Promise(resolve => setTimeout(resolve, 200));
            intentos++;
        }

        if (!window.googleMapsReady || !window.google || !google.maps) {
            throw new Error("Google Maps no terminó de cargar.");
        }
    },

    init: function (elementId, lat, lng, zoom) {
        const element = document.getElementById(elementId);
        if (!element) return;

        this.map = new google.maps.Map(element, {
            center: { lat: lat, lng: lng },
            zoom: zoom,
            mapTypeId: "roadmap",
            streetViewControl: false,
            fullscreenControl: false,
            mapTypeControl: false
        });

        this.dataLayer = new google.maps.Data({ map: this.map });
        this.marcadorTiempoReal = null;
        this.marcadorUsuario = null;
        this.infoTiempoReal = new google.maps.InfoWindow();
        this.puntoMarkers = [];
        this.todosLosPuntos = [];
        this.filtroActual = "ninguno";
        this.grupoSeleccionado = null;
        this.tipoSeleccionado = null;
    },

    drawGeoJson: function (geoJsonText) {
        if (!this.map || !this.dataLayer || !geoJsonText) return;

        this.dataLayer.forEach(feature => {
            this.dataLayer.remove(feature);
        });

        const data = typeof geoJsonText === "string"
            ? JSON.parse(geoJsonText)
            : geoJsonText;

        this.dataLayer.addGeoJson(data);

        this.dataLayer.setStyle({
            strokeColor: "#6A1B9A",
            strokeWeight: 5,
            fillOpacity: 0
        });

        const bounds = new google.maps.LatLngBounds();

        this.dataLayer.forEach(feature => {
            const geometry = feature.getGeometry();
            if (geometry) {
                this.procesarGeometria(geometry, bounds);
            }
        });

        if (!bounds.isEmpty()) {
            this.map.fitBounds(bounds);
        }
    },

    procesarGeometria: function (geometry, bounds) {
        if (geometry instanceof google.maps.LatLng) {
            bounds.extend(geometry);
            return;
        }

        if (geometry instanceof google.maps.Data.Point) {
            bounds.extend(geometry.get());
            return;
        }

        const array = geometry.getArray ? geometry.getArray() : [];
        for (let i = 0; i < array.length; i++) {
            this.procesarGeometria(array[i], bounds);
        }
    },

    normalizarTexto: function (valor) {
        return (valor || "").toString().trim().toLowerCase();
    },

    obtenerTipoPunto: function (punto) {
        const tipoOriginal = punto.Tipo ?? punto.tipo ?? "";
        const tipo = this.normalizarTexto(tipoOriginal);

        if (tipo.includes("cargadora")) return "cargadora";
        if (tipo.includes("cargador")) return "cargador";

        return "";
    },

    obtenerGrupoPunto: function (punto, index) {
        const fallbackIndex = punto?._indiceOriginal ?? index ?? 0;
        return (punto.Grupo ?? punto.grupo ?? `${fallbackIndex + 1}`).toString().trim();
    },

    crearIconoGrupo: function () {
        const svg = `
            <svg xmlns="http://www.w3.org/2000/svg" width="30" height="30" viewBox="0 0 30 30">
                <circle cx="15" cy="15" r="11.5" fill="#7B1FA2" stroke="#F3E5F5" stroke-width="2"/>
            </svg>
        `;

        return {
            url: "data:image/svg+xml;charset=UTF-8," + encodeURIComponent(svg),
            scaledSize: new google.maps.Size(30, 30),
            size: new google.maps.Size(30, 30),
            anchor: new google.maps.Point(15, 15),
            labelOrigin: new google.maps.Point(15, 15)
        };
    },

    limpiarPuntosRuta: function () {
        this.puntoMarkers.forEach(marker => marker.setMap(null));
        this.puntoMarkers = [];
    },

    drawPuntosRuta: function (puntosJson) {
        if (!this.map) return;

        try {
            const puntos = typeof puntosJson === "string"
                ? JSON.parse(puntosJson)
                : puntosJson;

            this.todosLosPuntos = (Array.isArray(puntos) ? puntos : []).map((p, index) => ({
                ...p,
                _indiceOriginal: index
            }));

            this.filtroActual = "ninguno";
            this.grupoSeleccionado = null;
            this.tipoSeleccionado = null;
            this.limpiarPuntosRuta();
        } catch (error) {
            console.error("Error al cargar puntos de ruta:", error);
        }
    },

    obtenerPuntosFiltrados: function () {
        if (!Array.isArray(this.todosLosPuntos)) return [];

        if (this.filtroActual === "ninguno") {
            return [];
        }

        if (this.filtroActual === "cargador") {
            return this.todosLosPuntos.filter(p => this.obtenerTipoPunto(p) === "cargador");
        }

        if (this.filtroActual === "cargadora") {
            return this.todosLosPuntos.filter(p => this.obtenerTipoPunto(p) === "cargadora");
        }

        if (this.filtroActual === "grupo-tipo" && this.tipoSeleccionado && this.grupoSeleccionado) {
            const grupoBuscado = this.normalizarTexto(this.grupoSeleccionado);

            return this.todosLosPuntos.filter((p, index) => {
                const grupo = this.obtenerGrupoPunto(p, index);
                return this.obtenerTipoPunto(p) === this.tipoSeleccionado &&
                    this.normalizarTexto(grupo) === grupoBuscado;
            });
        }

        return [];
    },

    redibujarPuntosFiltrados: function () {
        if (!this.map) return;

        this.limpiarPuntosRuta();

        const puntos = this.obtenerPuntosFiltrados();
        const iconoGrupo = this.crearIconoGrupo();

        puntos.forEach((punto, index) => {
            const lat = punto.Latitud ?? punto.latitud;
            const lng = punto.Longitud ?? punto.longitud;
            const grupo = this.obtenerGrupoPunto(punto, index);
            const referencia = punto.Referencia ?? punto.referencia ?? "";
            const tipo = this.obtenerTipoPunto(punto);

            if (lat == null || lng == null) return;

            const marker = new google.maps.Marker({
                position: { lat: lat, lng: lng },
                map: this.map,
                icon: iconoGrupo,
                title: `${tipo === "cargadora" ? "Cargadora" : tipo === "cargador" ? "Cargador" : "Grupo"} ${grupo}`,
                label: {
                    text: grupo,
                    color: "#FFFFFF",
                    fontSize: "10px",
                    fontWeight: "700"
                },
                zIndex: 900
            });

            const contenido = `
                <div style="min-width:150px">
                    <strong>Grupo: ${grupo || "Sin definir"}</strong>
                    ${tipo ? `<br/>Tipo: ${tipo === "cargadora" ? "Cargadora" : "Cargador"}` : ""}
                    ${referencia ? `<br/>Referencia: ${referencia}` : ""}
                </div>
            `;

            const info = new google.maps.InfoWindow({
                content: contenido
            });

            marker.addListener("click", () => {
                info.open(this.map, marker);
            });

            this.puntoMarkers.push(marker);
        });
    },

    ocultarTodosLosPuntos: function () {
        this.filtroActual = "ninguno";
        this.grupoSeleccionado = null;
        this.tipoSeleccionado = null;
        this.limpiarPuntosRuta();
        return true;
    },

    filtrarPorTipo: function (tipo) {
        const tipoNormalizado = this.normalizarTexto(tipo);

        if (tipoNormalizado === "cargadora") {
            this.filtroActual = "cargadora";
            this.tipoSeleccionado = "cargadora";
        } else {
            this.filtroActual = "cargador";
            this.tipoSeleccionado = "cargador";
        }

        this.grupoSeleccionado = null;
        this.redibujarPuntosFiltrados();
        return true;
    },

    obtenerGruposPorTipo: function (tipo) {
        const tipoNormalizado = this.normalizarTexto(tipo);

        const grupos = [...new Set(
            this.todosLosPuntos
                .filter(p => this.obtenerTipoPunto(p) === tipoNormalizado)
                .map((p, index) => this.obtenerGrupoPunto(p, index))
                .filter(g => g && g.trim() !== "")
        )];

        grupos.sort((a, b) => {
            const na = parseInt(a, 10);
            const nb = parseInt(b, 10);

            if (!isNaN(na) && !isNaN(nb)) {
                return na - nb;
            }

            return a.localeCompare(b);
        });

        return grupos;
    },

    filtrarPorGrupoYTipo: function (tipo, grupo) {
        if (!tipo || !grupo) return false;

        this.filtroActual = "grupo-tipo";
        this.tipoSeleccionado = this.normalizarTexto(tipo);
        this.grupoSeleccionado = grupo.toString().trim();
        this.redibujarPuntosFiltrados();
        return true;
    },

    actualizarMarcadorTiempoReal: function (lat, lng, grupoActual, mensaje, fechaHora) {
        if (!this.map) return;

        const position = { lat: lat, lng: lng };

        const iconoProcesion = {
            path: google.maps.SymbolPath.CIRCLE,
            scale: 10,
            fillColor: "#6A1B9A",
            fillOpacity: 1,
            strokeColor: "#ffffff",
            strokeWeight: 3
        };

        if (!this.marcadorTiempoReal) {
            this.marcadorTiempoReal = new google.maps.Marker({
                position: position,
                map: this.map,
                title: grupoActual || "Procesión",
                icon: iconoProcesion
            });

            this.marcadorTiempoReal.addListener("click", () => {
                this.infoTiempoReal.open(this.map, this.marcadorTiempoReal);
            });
        } else {
            this.marcadorTiempoReal.setPosition(position);
            this.marcadorTiempoReal.setTitle(grupoActual || "Procesión");
            this.marcadorTiempoReal.setIcon(iconoProcesion);
        }

        this.infoTiempoReal.setContent(`<div>
            <strong>${grupoActual || "Procesión"}</strong><br/>
            ${mensaje || ""}
        </div>`);
    },

    mostrarMiUbicacion: function (lat, lng, centrar = false) {
        if (!this.map) return;

        const position = { lat: lat, lng: lng };

        const iconoUsuario = {
            path: google.maps.SymbolPath.CIRCLE,
            scale: 9,
            fillColor: "#1E88E5",
            fillOpacity: 1,
            strokeColor: "#ffffff",
            strokeWeight: 3
        };

        if (!this.marcadorUsuario) {
            this.marcadorUsuario = new google.maps.Marker({
                position: position,
                map: this.map,
                title: "Mi ubicación",
                icon: iconoUsuario,
                zIndex: 1000
            });
        } else {
            this.marcadorUsuario.setPosition(position);
            this.marcadorUsuario.setIcon(iconoUsuario);
        }

        if (centrar) {
            this.map.panTo(position);
        }
    }
};