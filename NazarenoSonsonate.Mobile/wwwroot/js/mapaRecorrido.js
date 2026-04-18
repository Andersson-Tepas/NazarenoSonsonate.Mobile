window.mapaRecorrido = {
    map: null,
    dataLayer: null,
    marcadoresTiempoReal: {},
    marcadorUsuario: null,
    infoTiempoReal: null,
    infoPuntos: null,

    puntoMarkers: [],
    todosLosPuntos: [],
    puntosIndexados: [],
    visiblesActuales: [],
    renderVersion: 0,

    filtroActual: "ninguno",
    grupoSeleccionado: null,
    tipoSeleccionado: null,

    iconoGrupoCache: null,
    iconoJesusCache: null,
    iconoVirgenCache: null,
    iconoUsuarioCache: null,

    andasVisibles: true,
    ubicacionesTiempoReal: {},

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

        this.dispose(false);

        this.map = new google.maps.Map(element, {
            center: { lat, lng },
            zoom,
            mapTypeId: "roadmap",
            streetViewControl: false,
            fullscreenControl: false,
            mapTypeControl: false,
            gestureHandling: "greedy"
        });

        this.dataLayer = new google.maps.Data({ map: this.map });
        this.marcadoresTiempoReal = {};
        this.marcadorUsuario = null;
        this.infoTiempoReal = new google.maps.InfoWindow();
        this.infoPuntos = new google.maps.InfoWindow();

        this.puntoMarkers = [];
        this.todosLosPuntos = [];
        this.puntosIndexados = [];
        this.visiblesActuales = [];
        this.renderVersion = 0;

        this.filtroActual = "ninguno";
        this.grupoSeleccionado = null;
        this.tipoSeleccionado = null;

        this.iconoGrupoCache = null;
        this.iconoJesusCache = null;
        this.iconoVirgenCache = null;
        this.iconoUsuarioCache = null;

        this.andasVisibles = true;
        this.ubicacionesTiempoReal = {};
    },

    dispose: function (limpiarElemento = true) {
        this.renderVersion++;

        if (this.infoPuntos) {
            this.infoPuntos.close();
        }

        if (this.infoTiempoReal) {
            this.infoTiempoReal.close();
        }

        if (Array.isArray(this.puntosIndexados)) {
            for (let i = 0; i < this.puntosIndexados.length; i++) {
                const punto = this.puntosIndexados[i];
                if (punto && punto._marker) {
                    google.maps.event.clearInstanceListeners(punto._marker);
                    punto._marker.setMap(null);
                    punto._marker = null;
                    punto._visible = false;
                }
            }
        }

        if (this.marcadoresTiempoReal) {
            Object.values(this.marcadoresTiempoReal).forEach(marker => {
                if (marker) {
                    google.maps.event.clearInstanceListeners(marker);
                    marker.setMap(null);
                }
            });

            this.marcadoresTiempoReal = {};
        }

        if (this.marcadorUsuario) {
            google.maps.event.clearInstanceListeners(this.marcadorUsuario);
            this.marcadorUsuario.setMap(null);
            this.marcadorUsuario = null;
        }

        if (this.dataLayer) {
            this.dataLayer.forEach(feature => this.dataLayer.remove(feature));
            this.dataLayer.setMap(null);
            this.dataLayer = null;
        }

        this.puntoMarkers = [];
        this.todosLosPuntos = [];
        this.puntosIndexados = [];
        this.visiblesActuales = [];
        this.ubicacionesTiempoReal = {};

        this.filtroActual = "ninguno";
        this.grupoSeleccionado = null;
        this.tipoSeleccionado = null;
        this.andasVisibles = true;

        if (limpiarElemento && this.map && this.map.getDiv) {
            const div = this.map.getDiv();
            if (div) div.innerHTML = "";
        }

        this.map = null;
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
        if (this.iconoGrupoCache) return this.iconoGrupoCache;

        const svg = `
            <svg xmlns="http://www.w3.org/2000/svg" width="30" height="30" viewBox="0 0 30 30">
                <circle cx="15" cy="15" r="11.5" fill="#7B1FA2" stroke="#F3E5F5" stroke-width="2"/>
            </svg>
        `;

        this.iconoGrupoCache = {
            url: "data:image/svg+xml;charset=UTF-8," + encodeURIComponent(svg),
            scaledSize: new google.maps.Size(30, 30),
            size: new google.maps.Size(30, 30),
            anchor: new google.maps.Point(15, 15),
            labelOrigin: new google.maps.Point(15, 15)
        };

        return this.iconoGrupoCache;
    },

    obtenerIconoJesus: function () {
        if (this.iconoJesusCache) return this.iconoJesusCache;

        this.iconoJesusCache = {
            url: "images/jesus_icon.png",
            scaledSize: new google.maps.Size(28, 28), // 🔥 MÁS PEQUEÑO
            size: new google.maps.Size(28, 28),
            anchor: new google.maps.Point(14, 14)
        };

        return this.iconoJesusCache;
    },

    obtenerIconoVirgen: function () {
        if (this.iconoVirgenCache) return this.iconoVirgenCache;

        this.iconoVirgenCache = {
            url: "images/virgen_icon.png",
            scaledSize: new google.maps.Size(28, 28), // 🔥 MÁS PEQUEÑO
            size: new google.maps.Size(28, 28),
            anchor: new google.maps.Point(14, 14)
        };

        return this.iconoVirgenCache;
    },

    obtenerIconoUnidad: function (tipoUnidad) {
        const tipo = (tipoUnidad || "").toString().trim();

        if (tipo === "VirgenMaria") {
            return this.obtenerIconoVirgen();
        }

        return this.obtenerIconoJesus();
    },

    obtenerTituloUnidad: function (tipoUnidad) {
        return tipoUnidad === "VirgenMaria"
            ? "Anda Virgen María"
            : "Anda Jesús Nazareno";
    },

    limpiarPuntosRuta: function () {
        this.renderVersion++;

        if (this.infoPuntos) {
            this.infoPuntos.close();
        }

        for (let i = 0; i < this.puntosIndexados.length; i++) {
            const punto = this.puntosIndexados[i];
            if (punto && punto._marker) {
                google.maps.event.clearInstanceListeners(punto._marker);
                punto._marker.setMap(null);
                punto._marker = null;
                punto._visible = false;
            }
        }

        this.puntoMarkers = [];
        this.todosLosPuntos = [];
        this.puntosIndexados = [];
        this.visiblesActuales = [];
    },

    drawPuntosRuta: function (puntosData) {
        if (!this.map) return;

        try {
            const puntos = Array.isArray(puntosData)
                ? puntosData
                : (typeof puntosData === "string" ? JSON.parse(puntosData) : []);

            this.limpiarPuntosRuta();

            this.todosLosPuntos = Array.isArray(puntos) ? puntos : [];

            this.puntosIndexados = this.todosLosPuntos
                .map((p, index) => {
                    const tipo = this.obtenerTipoPunto(p);
                    const grupo = this.obtenerGrupoPunto(p, index);

                    const latRaw = p.Latitud ?? p.latitud;
                    const lngRaw = p.Longitud ?? p.longitud;

                    const lat = Number(latRaw);
                    const lng = Number(lngRaw);

                    return {
                        ...p,
                        _indiceOriginal: index,
                        _tipoNormalizado: tipo,
                        _grupoTexto: grupo,
                        _grupoNormalizado: this.normalizarTexto(grupo),
                        _lat: Number.isFinite(lat) ? lat : null,
                        _lng: Number.isFinite(lng) ? lng : null,
                        _marker: null,
                        _visible: false,
                        _markerKey: `${tipo}|${this.normalizarTexto(grupo)}|${index}`
                    };
                })
                .filter(p => p._lat !== null && p._lng !== null);

            this.filtroActual = "ninguno";
            this.grupoSeleccionado = null;
            this.tipoSeleccionado = null;
        } catch (error) {
            console.error("Error al cargar puntos de ruta:", error);
            this.limpiarPuntosRuta();
        }
    },

    crearMarkerParaPunto: function (punto) {
        if (punto._marker) return punto._marker;

        const tipo = punto._tipoNormalizado;
        const grupo = punto._grupoTexto;
        const referencia = punto.Referencia ?? punto.referencia ?? "";

        const marker = new google.maps.Marker({
            position: { lat: punto._lat, lng: punto._lng },
            map: null,
            icon: this.crearIconoGrupo(),
            title: `${tipo === "cargadora" ? "Cargadora" : tipo === "cargador" ? "Cargador" : "Grupo"} ${grupo}`,
            label: {
                text: grupo,
                color: "#FFFFFF",
                fontSize: "10px",
                fontWeight: "700"
            },
            zIndex: 900,
            optimized: true
        });

        marker.addListener("click", () => {
            const contenido = `
                <div style="min-width:150px">
                    <strong>Grupo: ${grupo || "Sin definir"}</strong>
                    ${tipo ? `<br/>Tipo: ${tipo === "cargadora" ? "Cargadora" : "Cargador"}` : ""}
                    ${referencia ? `<br/>Referencia: ${referencia}` : ""}
                </div>
            `;

            this.infoPuntos.setContent(contenido);
            this.infoPuntos.open(this.map, marker);
        });

        punto._marker = marker;
        this.puntoMarkers.push(marker);

        return marker;
    },

    obtenerPuntosFiltrados: function () {
        if (!Array.isArray(this.puntosIndexados)) return [];

        if (this.filtroActual === "ninguno") {
            return [];
        }

        if (this.filtroActual === "cargador") {
            return this.puntosIndexados.filter(p => p._tipoNormalizado === "cargador");
        }

        if (this.filtroActual === "cargadora") {
            return this.puntosIndexados.filter(p => p._tipoNormalizado === "cargadora");
        }

        if (this.filtroActual === "grupo-tipo" && this.tipoSeleccionado && this.grupoSeleccionado) {
            const grupoBuscado = this.normalizarTexto(this.grupoSeleccionado);

            return this.puntosIndexados.filter(p =>
                p._tipoNormalizado === this.tipoSeleccionado &&
                p._grupoNormalizado === grupoBuscado
            );
        }

        return [];
    },

    sincronizarPuntosVisibles: function (puntosVisibles, version) {
        if (!this.map) return Promise.resolve(false);

        const nuevasKeys = new Set(puntosVisibles.map(p => p._markerKey));

        if (this.infoPuntos) {
            this.infoPuntos.close();
        }

        for (let i = 0; i < this.puntosIndexados.length; i++) {
            const punto = this.puntosIndexados[i];
            if (punto._visible && !nuevasKeys.has(punto._markerKey) && punto._marker) {
                punto._marker.setMap(null);
                punto._visible = false;
            }
        }

        this.visiblesActuales = puntosVisibles;

        const lote = 35;
        let index = 0;

        return new Promise(resolve => {
            const procesar = () => {
                if (version !== this.renderVersion || !this.map) {
                    resolve(false);
                    return;
                }

                const limite = Math.min(index + lote, puntosVisibles.length);

                for (; index < limite; index++) {
                    const punto = puntosVisibles[index];

                    if (!punto._visible) {
                        const marker = this.crearMarkerParaPunto(punto);
                        marker.setMap(this.map);
                        punto._visible = true;
                    }
                }

                if (index < puntosVisibles.length) {
                    requestAnimationFrame(procesar);
                } else {
                    resolve(true);
                }
            };

            requestAnimationFrame(procesar);
        });
    },

    redibujarPuntosFiltrados: async function () {
        if (!this.map) return false;

        const version = ++this.renderVersion;
        const puntos = this.obtenerPuntosFiltrados();
        await this.sincronizarPuntosVisibles(puntos, version);
        return true;
    },

    ocultarTodosLosPuntos: async function () {
        this.filtroActual = "ninguno";
        this.grupoSeleccionado = null;
        this.tipoSeleccionado = null;
        await this.redibujarPuntosFiltrados();
        return true;
    },

    filtrarPorTipo: async function (tipo) {
        const tipoNormalizado = this.normalizarTexto(tipo);

        if (tipoNormalizado === "cargadora") {
            this.filtroActual = "cargadora";
            this.tipoSeleccionado = "cargadora";
        } else {
            this.filtroActual = "cargador";
            this.tipoSeleccionado = "cargador";
        }

        this.grupoSeleccionado = null;
        await this.redibujarPuntosFiltrados();
        return true;
    },

    obtenerGruposPorTipo: function (tipo) {
        const tipoNormalizado = this.normalizarTexto(tipo);

        const grupos = [...new Set(
            this.puntosIndexados
                .filter(p => p._tipoNormalizado === tipoNormalizado)
                .map(p => p._grupoTexto)
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

    filtrarPorGrupoYTipo: async function (tipo, grupo) {
        if (!tipo || !grupo) return false;

        this.filtroActual = "grupo-tipo";
        this.tipoSeleccionado = this.normalizarTexto(tipo);
        this.grupoSeleccionado = grupo.toString().trim();

        await this.redibujarPuntosFiltrados();
        return true;
    },

    registrarUbicacionTiempoReal: function (tipoUnidad, lat, lng, grupoActual, mensaje, fechaHora) {
        if (!tipoUnidad) return;

        this.ubicacionesTiempoReal[tipoUnidad] = {
            tipoUnidad,
            lat,
            lng,
            grupoActual,
            mensaje,
            fechaHora
        };

        if (this.andasVisibles) {
            this.renderizarAnda(tipoUnidad);
        }
    },

    renderizarAnda: function (tipoUnidad) {
        if (!this.map || !tipoUnidad) return;

        const data = this.ubicacionesTiempoReal[tipoUnidad];
        if (!data) return;

        const position = { lat: data.lat, lng: data.lng };
        let marker = this.marcadoresTiempoReal[tipoUnidad];
        const titulo = this.obtenerTituloUnidad(tipoUnidad);

        if (!marker) {
            marker = new google.maps.Marker({
                position,
                map: this.map,
                title: titulo,
                icon: this.obtenerIconoUnidad(tipoUnidad),
                zIndex: 1100,
                optimized: true
            });

            marker.addListener("click", () => {
                const actual = this.ubicacionesTiempoReal[tipoUnidad];

                this.infoTiempoReal.setContent(`
                    <div style="min-width:170px">
                        <strong>${titulo}</strong><br/>
                        ${actual?.grupoActual || ""}${actual?.grupoActual ? "<br/>" : ""}
                        ${actual?.mensaje || ""}${actual?.mensaje ? "<br/>" : ""}
                        ${actual?.fechaHora || ""}
                    </div>
                `);

                this.infoTiempoReal.open(this.map, marker);
            });

            this.marcadoresTiempoReal[tipoUnidad] = marker;
        } else {
            marker.setPosition(position);
            marker.setTitle(titulo);
            marker.setMap(this.map);
        }
    },

    ocultarAndasTiempoReal: function () {
        this.andasVisibles = false;

        Object.values(this.marcadoresTiempoReal).forEach(marker => {
            if (marker) {
                marker.setMap(null);
            }
        });
    },

    mostrarAndasTiempoReal: function () {
        this.andasVisibles = true;

        Object.keys(this.ubicacionesTiempoReal).forEach(tipoUnidad => {
            this.renderizarAnda(tipoUnidad);
        });
    },

    mostrarMiUbicacion: function (lat, lng, centrar = false) {
        if (!this.map) return;

        const position = { lat, lng };

        if (!this.marcadorUsuario) {
            this.marcadorUsuario = new google.maps.Marker({
                position,
                map: this.map,
                title: "Mi ubicación",
                icon: this.obtenerIconoUsuario(),
                zIndex: 1200,
                optimized: true
            });
        } else {
            this.marcadorUsuario.setPosition(position);
        }

        if (centrar) {
            this.map.panTo(position);
        }
    },

    obtenerIconoUsuario: function () {
        if (this.iconoUsuarioCache) return this.iconoUsuarioCache;

        this.iconoUsuarioCache = {
            path: google.maps.SymbolPath.CIRCLE,
            scale: 9,
            fillColor: "#1E88E5",
            fillOpacity: 1,
            strokeColor: "#ffffff",
            strokeWeight: 3
        };

        return this.iconoUsuarioCache;
    }
};