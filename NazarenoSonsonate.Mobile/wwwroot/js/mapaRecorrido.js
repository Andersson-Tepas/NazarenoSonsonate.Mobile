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
    animacionesTiempoReal: {},
    autoCentrarProcesion: true,

    andaInfoActivaStorageKey: "nazareno_anda_info_activa",
    globoAndaOverlay: null,
    globoAndaTipoUnidad: null,

    globoPuntoOverlay: null,

    esperarGoogleMaps: async function () {
        let intentos = 0;

        while ((!window.googleMapsReady || !window.google || !google.maps) && intentos < 50) {
            await new Promise(resolve => setTimeout(resolve, 200));
            intentos++;
        }

        if (!window.googleMapsReady || !window.google || !google.maps) {
            console.warn("Google Maps no terminó de cargar.");
            return false;
        }

        return true;
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
        this.infoTiempoReal = null;
        this.infoPuntos = null;

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
        this.animacionesTiempoReal = {};
        this.autoCentrarProcesion = true;

        this.cerrarGloboAnda(false);
        this.cerrarGloboPunto();
    },

    dispose: function (limpiarElemento = true) {
        this.renderVersion++;

        this.cerrarGloboAnda(false);
        this.cerrarGloboPunto();

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

        if (this.animacionesTiempoReal) {
            Object.values(this.animacionesTiempoReal).forEach(animacion => {
                if (animacion && animacion.frameId) {
                    cancelAnimationFrame(animacion.frameId);
                }
            });

            this.animacionesTiempoReal = {};
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
        this.autoCentrarProcesion = true;

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

        this.dataLayer.setStyle((feature) => {
            const ruta = (feature.getProperty("ruta") || feature.getProperty("tipo") || feature.getProperty("nombre") || "")
                .toString()
                .trim()
                .toLowerCase();

            const colorProp = (feature.getProperty("color") || "")
                .toString()
                .trim()
                .toLowerCase();

            const esVirgen =
                ruta.includes("virgen") ||
                colorProp === "#ffd600" ||
                colorProp === "amarillo" ||
                colorProp === "yellow";

            return {
                strokeColor: esVirgen ? "#FFD600" : "#6A1B9A",
                strokeWeight: 5,
                strokeOpacity: 1,
                fillOpacity: 0
            };
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
            scaledSize: new google.maps.Size(28, 28),
            size: new google.maps.Size(28, 28),
            anchor: new google.maps.Point(14, 14)
        };

        return this.iconoJesusCache;
    },

    obtenerIconoVirgen: function () {
        if (this.iconoVirgenCache) return this.iconoVirgenCache;

        this.iconoVirgenCache = {
            url: "images/virgen_icon.png",
            scaledSize: new google.maps.Size(28, 28),
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
            ? "Virgen María"
            : "Jesús Nazareno";
    },

    guardarAndaInfoActiva: function (tipoUnidad) {
        try {
            localStorage.setItem(this.andaInfoActivaStorageKey, tipoUnidad || "");
        } catch {
        }
    },

    obtenerAndaInfoActiva: function () {
        try {
            return localStorage.getItem(this.andaInfoActivaStorageKey) || "";
        } catch {
            return "";
        }
    },

    crearGloboOverlay: function (marker, contenidoHtml, anchoMaximo = "220px") {
        const overlay = new google.maps.OverlayView();

        overlay._marker = marker;
        overlay._contenidoHtml = contenidoHtml;
        overlay._div = null;

        overlay.onAdd = function () {
            const contenedor = document.createElement("div");
            contenedor.style.position = "absolute";
            contenedor.style.transform = "translate(-50%, -100%)";
            contenedor.style.marginTop = "-12px";
            contenedor.style.pointerEvents = "auto";
            contenedor.style.zIndex = "9999";

            const caja = document.createElement("div");
            caja.innerHTML = contenidoHtml;
            caja.style.background = "#ffffff";
            caja.style.color = "#111111";
            caja.style.border = "1px solid #8a8a8a";
            caja.style.borderRadius = "0px";
            caja.style.padding = "5px 10px";
            caja.style.fontSize = "14px";
            caja.style.fontWeight = "400";
            caja.style.lineHeight = "19px";
            caja.style.whiteSpace = "nowrap";
            caja.style.maxWidth = anchoMaximo;
            caja.style.boxShadow = "0 1px 2px rgba(0,0,0,0.25)";
            caja.style.fontFamily = "Arial, sans-serif";

            const flecha = document.createElement("div");
            flecha.style.position = "absolute";
            flecha.style.left = "50%";
            flecha.style.bottom = "-6px";
            flecha.style.width = "10px";
            flecha.style.height = "10px";
            flecha.style.background = "#ffffff";
            flecha.style.borderRight = "1px solid #8a8a8a";
            flecha.style.borderBottom = "1px solid #8a8a8a";
            flecha.style.transform = "translateX(-50%) rotate(45deg)";
            flecha.style.boxShadow = "1px 1px 1px rgba(0,0,0,0.10)";

            contenedor.appendChild(caja);
            contenedor.appendChild(flecha);

            this._div = contenedor;

            const panes = this.getPanes();
            panes.floatPane.appendChild(contenedor);
        };

        overlay.draw = function () {
            if (!this._div || !this._marker) return;

            const projection = this.getProjection();
            if (!projection) return;

            const position = this._marker.getPosition();
            if (!position) return;

            const point = projection.fromLatLngToDivPixel(position);
            if (!point) return;

            this._div.style.left = point.x + "px";
            this._div.style.top = point.y + "px";
        };

        overlay.onRemove = function () {
            if (this._div && this._div.parentNode) {
                this._div.parentNode.removeChild(this._div);
            }

            this._div = null;
        };

        overlay.setMap(this.map);
        return overlay;
    },

    cerrarGloboAnda: function (limpiarPersistencia = false) {
        if (this.globoAndaOverlay) {
            try {
                this.globoAndaOverlay.setMap(null);
            } catch {
            }
        }

        this.globoAndaOverlay = null;
        this.globoAndaTipoUnidad = null;

        if (limpiarPersistencia) {
            try {
                localStorage.removeItem(this.andaInfoActivaStorageKey);
            } catch {
            }
        }
    },

    cerrarGloboPunto: function () {
        if (this.globoPuntoOverlay) {
            try {
                this.globoPuntoOverlay.setMap(null);
            } catch {
            }
        }

        this.globoPuntoOverlay = null;
    },

    abrirGloboAnda: function (tipoUnidad, marker) {
        if (!this.map || !marker || !tipoUnidad) return;

        this.cerrarGloboPunto();
        this.cerrarGloboAnda(false);

        const titulo = this.obtenerTituloUnidad(tipoUnidad);
        const contenido = `<strong style="font-size:15px;font-weight:700;">${titulo}</strong>`;

        this.globoAndaOverlay = this.crearGloboOverlay(marker, contenido, "180px");
        this.globoAndaTipoUnidad = tipoUnidad;
    },

    abrirGloboPunto: function (marker, grupo, tipo, referencia) {
        if (!this.map || !marker) return;

        this.cerrarGloboAnda(false);
        this.cerrarGloboPunto();

        const tipoTexto = tipo === "cargadora"
            ? "Cargadora"
            : tipo === "cargador"
                ? "Cargador"
                : "Grupo";

        const referenciaTexto = referencia && referencia.toString().trim() !== ""
            ? referencia.toString().trim()
            : "Sin referencia";

        const contenido = `
            <div style="font-size:14px; line-height:19px;">
                <strong style="font-size:15px;">Grupo: ${grupo || "Sin definir"}</strong><br/>
                <span>Tipo: ${tipoTexto}</span><br/>
                <span>Referencia: ${referenciaTexto}</span>
            </div>
        `;

        this.globoPuntoOverlay = this.crearGloboOverlay(marker, contenido, "240px");
    },

    actualizarGloboAnda: function () {
        if (this.globoAndaOverlay && typeof this.globoAndaOverlay.draw === "function") {
            this.globoAndaOverlay.draw();
        }
    },

    limpiarPuntosRuta: function () {
        this.renderVersion++;
        this.cerrarGloboPunto();

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
            this.abrirGloboPunto(marker, grupo, tipo, referencia);
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

        this.cerrarGloboPunto();

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
        this.cerrarGloboPunto();
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
        this.cerrarGloboPunto();
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

        this.cerrarGloboPunto();
        await this.redibujarPuntosFiltrados();
        return true;
    },

    registrarUbicacionTiempoReal: function (tipoUnidad, lat, lng, grupoActual, mensaje, fechaHora) {
        if (!tipoUnidad) return;

        const latNum = Number(lat);
        const lngNum = Number(lng);

        if (!Number.isFinite(latNum) || !Number.isFinite(lngNum)) return;
        if (latNum === 0 && lngNum === 0) return;

        const ubicacionAnterior = this.ubicacionesTiempoReal[tipoUnidad];

        if (ubicacionAnterior) {
            const distancia = this.calcularDistanciaMetros(
                ubicacionAnterior.lat,
                ubicacionAnterior.lng,
                latNum,
                lngNum
            );

            if (distancia > 120) {
                return;
            }
        }

        this.ubicacionesTiempoReal[tipoUnidad] = {
            tipoUnidad,
            lat: latNum,
            lng: lngNum,
            grupoActual,
            mensaje,
            fechaHora
        };

        if (!this.andasVisibles) return;

        if (!ubicacionAnterior) {
            this.renderizarAnda(tipoUnidad, true);
            return;
        }

        this.animarAnda(
            tipoUnidad,
            ubicacionAnterior.lat,
            ubicacionAnterior.lng,
            latNum,
            lngNum
        );
    },

    renderizarAnda: function (tipoUnidad, centrar = false) {
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
                this.guardarAndaInfoActiva(tipoUnidad);
                this.abrirGloboAnda(tipoUnidad, marker);
            });

            this.marcadoresTiempoReal[tipoUnidad] = marker;
        } else {
            marker.setPosition(position);
            marker.setTitle(titulo);
            marker.setMap(this.map);
        }

        const andaInfoActiva = this.obtenerAndaInfoActiva();

        if (andaInfoActiva === tipoUnidad) {
            setTimeout(() => {
                this.abrirGloboAnda(tipoUnidad, marker);
            }, 150);
        }

        if (centrar && this.autoCentrarProcesion) {
            this.map.panTo(position);
        }
    },

    animarAnda: function (tipoUnidad, fromLat, fromLng, toLat, toLng) {
        if (!this.map) return;

        const marker = this.marcadoresTiempoReal[tipoUnidad];
        if (!marker) {
            this.renderizarAnda(tipoUnidad, true);
            return;
        }

        const animacionAnterior = this.animacionesTiempoReal[tipoUnidad];
        if (animacionAnterior && animacionAnterior.frameId) {
            cancelAnimationFrame(animacionAnterior.frameId);
        }

        const duracion = 3000;
        const inicio = performance.now();
        const centrarDuranteAnimacion = this.andasVisibles && this.autoCentrarProcesion;

        const step = (ahora) => {
            const transcurrido = ahora - inicio;
            const progreso = Math.min(transcurrido / duracion, 1);

            const easing = 1 - Math.pow(1 - progreso, 3);

            const latActual = fromLat + (toLat - fromLat) * easing;
            const lngActual = fromLng + (toLng - fromLng) * easing;
            const position = { lat: latActual, lng: lngActual };

            marker.setPosition(position);

            if (this.globoAndaTipoUnidad === tipoUnidad) {
                this.actualizarGloboAnda();
            }

            if (centrarDuranteAnimacion) {
                this.map.panTo(position);
            }

            if (progreso < 1) {
                this.animacionesTiempoReal[tipoUnidad] = {
                    frameId: requestAnimationFrame(step)
                };
            } else {
                marker.setPosition({ lat: toLat, lng: toLng });

                if (this.globoAndaTipoUnidad === tipoUnidad) {
                    this.actualizarGloboAnda();
                }

                if (centrarDuranteAnimacion) {
                    this.map.panTo({ lat: toLat, lng: toLng });
                }

                delete this.animacionesTiempoReal[tipoUnidad];
            }
        };

        this.animacionesTiempoReal[tipoUnidad] = {
            frameId: requestAnimationFrame(step)
        };
    },

    actualizarGloboAnda: function () {
        if (this.globoAndaOverlay && typeof this.globoAndaOverlay.draw === "function") {
            this.globoAndaOverlay.draw();
        }
    },

    ocultarAndasTiempoReal: function () {
        this.andasVisibles = false;
        this.cerrarGloboAnda(false);

        Object.values(this.marcadoresTiempoReal).forEach(marker => {
            if (marker) {
                marker.setMap(null);
            }
        });
    },

    mostrarAndasTiempoReal: function () {
        this.andasVisibles = true;

        Object.keys(this.ubicacionesTiempoReal).forEach(tipoUnidad => {
            this.renderizarAnda(tipoUnidad, true);
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
    },

    calcularDistanciaMetros: function (lat1, lng1, lat2, lng2) {
        const toRad = (deg) => deg * Math.PI / 180;
        const R = 6371000;

        const dLat = toRad(lat2 - lat1);
        const dLng = toRad(lng2 - lng1);

        const a =
            Math.sin(dLat / 2) * Math.sin(dLat / 2) +
            Math.cos(toRad(lat1)) * Math.cos(toRad(lat2)) *
            Math.sin(dLng / 2) * Math.sin(dLng / 2);

        const c = 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));

        return R * c;
    }
};