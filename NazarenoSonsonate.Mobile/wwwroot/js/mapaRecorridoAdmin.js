window.mapaRecorridoAdmin = {
    map: null,
    polylineJesus: null,
    polylineVirgen: null,
    rutaActiva: "jesus",
    markers: [],
    checkpointMarkers: [],
    checkpoints: [],
    modoCheckpoint: false,
    infoWindowCheckpoint: null,
    mapClickListener: null,

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

    normalizarTipo: function (tipo) {
        const valor = (tipo || "").toString().trim().toLowerCase();

        if (valor.includes("cargadora")) return "Cargadora";
        if (valor.includes("cargador")) return "Cargador";

        return "Cargador";
    },

    obtenerPolylineActiva: function () {
        return this.rutaActiva === "virgen"
            ? this.polylineVirgen
            : this.polylineJesus;
    },

    actualizarEdicionRutas: function () {
        if (this.polylineJesus) {
            this.polylineJesus.setEditable(this.rutaActiva === "jesus");
            this.polylineJesus.setOptions({
                strokeOpacity: this.rutaActiva === "jesus" ? 1 : 0.75,
                strokeWeight: this.rutaActiva === "jesus" ? 6 : 5
            });
        }

        if (this.polylineVirgen) {
            this.polylineVirgen.setEditable(this.rutaActiva === "virgen");
            this.polylineVirgen.setOptions({
                strokeOpacity: this.rutaActiva === "virgen" ? 1 : 0.85,
                strokeWeight: this.rutaActiva === "virgen" ? 6 : 5
            });
        }
    },

    seleccionarRutaJesus: function () {
        this.rutaActiva = "jesus";
        this.actualizarEdicionRutas();
        this.redibujarMarcadoresRuta();
    },

    seleccionarRutaVirgen: function () {
        this.rutaActiva = "virgen";
        this.actualizarEdicionRutas();
        this.redibujarMarcadoresRuta();
    },

    init: function (elementId, lat, lng, zoom, geoJsonText, checkpointsJson) {
        const element = document.getElementById(elementId);
        if (!element) return;

        this.dispose(false);

        this.map = new google.maps.Map(element, {
            center: { lat: lat, lng: lng },
            zoom: zoom,
            mapTypeId: "roadmap",
            streetViewControl: false,
            fullscreenControl: false,
            mapTypeControl: false,
            gestureHandling: "greedy"
        });

        this.polylineJesus = new google.maps.Polyline({
            map: this.map,
            path: [],
            strokeColor: "#8E24AA",
            strokeOpacity: 1,
            strokeWeight: 5,
            editable: true
        });

        this.polylineVirgen = new google.maps.Polyline({
            map: this.map,
            path: [],
            strokeColor: "#FFD600",
            strokeOpacity: 0.85,
            strokeWeight: 5,
            editable: false
        });

        this.rutaActiva = "jesus";
        this.markers = [];
        this.checkpointMarkers = [];
        this.checkpoints = [];
        this.modoCheckpoint = false;
        this.infoWindowCheckpoint = new google.maps.InfoWindow();

        if (geoJsonText) {
            this.cargarGeoJson(geoJsonText);
        }

        if (checkpointsJson) {
            this.cargarCheckpoints(checkpointsJson);
        }

        this.actualizarEdicionRutas();

        this.mapClickListener = this.map.addListener("click", (event) => {
            if (!event.latLng) return;

            if (this.modoCheckpoint) {
                const referencia = prompt("Referencia del punto:", "");
                if (referencia === null) {
                    this.modoCheckpoint = false;
                    return;
                }

                const grupo = prompt("¿Qué grupo va ahí?", "");
                if (grupo === null) {
                    this.modoCheckpoint = false;
                    return;
                }

                const tipoIngresado = prompt("Tipo del punto (Cargador/Cargadora):", "Cargador");
                if (tipoIngresado === null) {
                    this.modoCheckpoint = false;
                    return;
                }

                this.checkpoints.push({
                    Id: 0,
                    RecorridoId: 0,
                    Latitud: event.latLng.lat(),
                    Longitud: event.latLng.lng(),
                    Orden: this.checkpoints.length + 1,
                    Referencia: referencia,
                    Grupo: grupo,
                    Tipo: this.normalizarTipo(tipoIngresado)
                });

                this.redibujarCheckpoints();
                this.modoCheckpoint = false;
                return;
            }

            const polylineActiva = this.obtenerPolylineActiva();
            if (!polylineActiva) return;

            polylineActiva.getPath().push(event.latLng);
            this.redibujarMarcadoresRuta();
        });
    },

    activarModoCheckpoint: function () {
        this.modoCheckpoint = true;
    },

    cargarGeoJson: function (geoJsonText) {
        try {
            const data = typeof geoJsonText === "string" ? JSON.parse(geoJsonText) : geoJsonText;

            const pathJesus = this.polylineJesus.getPath();
            const pathVirgen = this.polylineVirgen.getPath();
            pathJesus.clear();
            pathVirgen.clear();

            const bounds = new google.maps.LatLngBounds();

            const agregarCoordenadas = (path, coordinates) => {
                coordinates.forEach(coord => {
                    const lng = Number(coord[0]);
                    const lat = Number(coord[1]);

                    if (!Number.isFinite(lat) || !Number.isFinite(lng)) return;

                    const latLng = new google.maps.LatLng(lat, lng);
                    path.push(latLng);
                    bounds.extend(latLng);
                });
            };

            if (data.type === "FeatureCollection" && Array.isArray(data.features)) {
                const lineFeatures = data.features.filter(f =>
                    f?.geometry?.type === "LineString" && Array.isArray(f.geometry.coordinates)
                );

                if (lineFeatures.length === 1) {
                    agregarCoordenadas(pathJesus, lineFeatures[0].geometry.coordinates || []);
                } else {
                    lineFeatures.forEach(feature => {
                        const ruta =
                            (feature.properties?.ruta || feature.properties?.tipo || feature.properties?.nombre || "")
                                .toString()
                                .trim()
                                .toLowerCase();

                        const color =
                            (feature.properties?.color || "")
                                .toString()
                                .trim()
                                .toLowerCase();

                        const esVirgen =
                            ruta.includes("virgen") ||
                            color === "#ffd600" ||
                            color === "yellow" ||
                            color === "amarillo";

                        if (esVirgen) {
                            agregarCoordenadas(pathVirgen, feature.geometry.coordinates || []);
                        } else {
                            agregarCoordenadas(pathJesus, feature.geometry.coordinates || []);
                        }
                    });
                }
            } else if (data.type === "Feature" && data.geometry?.type === "LineString") {
                agregarCoordenadas(pathJesus, data.geometry.coordinates || []);
            } else if (data.type === "LineString") {
                agregarCoordenadas(pathJesus, data.coordinates || []);
            }

            this.redibujarMarcadoresRuta();

            if (!bounds.isEmpty()) {
                this.map.fitBounds(bounds);
            }
        } catch (e) {
            console.error("Error cargando GeoJSON:", e);
        }
    },

    cargarCheckpoints: function (checkpointsJson) {
        try {
            const puntos = typeof checkpointsJson === "string"
                ? JSON.parse(checkpointsJson)
                : checkpointsJson;

            this.checkpoints = Array.isArray(puntos)
                ? puntos.map(p => ({
                    Id: p.Id ?? p.id ?? 0,
                    RecorridoId: p.RecorridoId ?? p.recorridoId ?? 0,
                    Latitud: Number(p.Latitud ?? p.latitud ?? 0),
                    Longitud: Number(p.Longitud ?? p.longitud ?? 0),
                    Orden: p.Orden ?? p.orden ?? 0,
                    Referencia: p.Referencia ?? p.referencia ?? "",
                    Grupo: p.Grupo ?? p.grupo ?? "",
                    Tipo: this.normalizarTipo(p.Tipo ?? p.tipo ?? "Cargador")
                }))
                : [];

            this.redibujarCheckpoints();
        } catch (e) {
            console.error("Error cargando puntos:", e);
        }
    },

    redibujarMarcadoresRuta: function () {
        this.markers.forEach(m => {
            google.maps.event.clearInstanceListeners(m);
            m.setMap(null);
        });
        this.markers = [];

        const polylineActiva = this.obtenerPolylineActiva();
        if (!polylineActiva) return;

        const path = polylineActiva.getPath();

        for (let i = 0; i < path.getLength(); i++) {
            const point = path.getAt(i);

            const marker = new google.maps.Marker({
                position: point,
                map: this.map,
                label: `${i + 1}`,
                icon: {
                    url: this.rutaActiva === "virgen"
                        ? "http://maps.google.com/mapfiles/ms/icons/orange-dot.png"
                        : "http://maps.google.com/mapfiles/ms/icons/yellow-dot.png"
                }
            });

            marker.addListener("click", () => {
                const pathActual = this.obtenerPolylineActiva().getPath();
                if (i < pathActual.getLength()) {
                    pathActual.removeAt(i);
                    this.redibujarMarcadoresRuta();
                }
            });

            this.markers.push(marker);
        }
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

    redibujarCheckpoints: function () {
        this.checkpointMarkers.forEach(m => {
            google.maps.event.clearInstanceListeners(m);
            m.setMap(null);
        });
        this.checkpointMarkers = [];

        const iconoGrupo = this.crearIconoGrupo();

        this.checkpoints.forEach((punto, index) => {
            punto.Orden = index + 1;
            punto.Tipo = this.normalizarTipo(punto.Tipo);

            const textoGrupo = (punto.Grupo || `${index + 1}`).toString().trim();

            const marker = new google.maps.Marker({
                position: { lat: punto.Latitud, lng: punto.Longitud },
                map: this.map,
                draggable: true,
                title: `${punto.Tipo} ${punto.Grupo || punto.Referencia || `Punto ${index + 1}`}`,
                icon: iconoGrupo,
                label: {
                    text: textoGrupo,
                    color: "#FFFFFF",
                    fontSize: "10px",
                    fontWeight: "700"
                },
                zIndex: 1000
            });

            const construirContenido = () => `
                <div style="min-width:170px">
                    <strong>Grupo: ${punto.Grupo || "Sin definir"}</strong>
                    <br/>Tipo: ${punto.Tipo || "Cargador"}
                    ${punto.Referencia ? `<br/>Referencia: ${punto.Referencia}` : ""}
                </div>`;

            marker.addListener("click", () => {
                const nuevaReferencia = prompt("Editar referencia:", punto.Referencia || "");
                if (nuevaReferencia === null) return;

                const nuevoGrupo = prompt("Editar grupo:", punto.Grupo || "");
                if (nuevoGrupo === null) return;

                const nuevoTipo = prompt("Editar tipo (Cargador/Cargadora):", punto.Tipo || "Cargador");
                if (nuevoTipo === null) return;

                punto.Referencia = nuevaReferencia;
                punto.Grupo = nuevoGrupo;
                punto.Tipo = this.normalizarTipo(nuevoTipo);

                marker.setLabel({
                    text: (punto.Grupo || `${index + 1}`).toString().trim(),
                    color: "#FFFFFF",
                    fontSize: "10px",
                    fontWeight: "700"
                });

                marker.setTitle(`${punto.Tipo} ${punto.Grupo || punto.Referencia || `Punto ${index + 1}`}`);
            });

            marker.addListener("dblclick", () => {
                this.infoWindowCheckpoint.setContent(construirContenido());
                this.infoWindowCheckpoint.open(this.map, marker);
            });

            marker.addListener("dragend", (event) => {
                punto.Latitud = event.latLng.lat();
                punto.Longitud = event.latLng.lng();
            });

            marker.addListener("rightclick", () => {
                this.checkpoints.splice(index, 1);
                this.redibujarCheckpoints();
            });

            this.checkpointMarkers.push(marker);
        });
    },

    deshacerUltimoPunto: function () {
        const polylineActiva = this.obtenerPolylineActiva();
        if (!polylineActiva) return;

        const path = polylineActiva.getPath();
        const length = path.getLength();

        if (length > 0) {
            path.removeAt(length - 1);
            this.redibujarMarcadoresRuta();
        }
    },

    limpiarRutaActiva: function () {
        const polylineActiva = this.obtenerPolylineActiva();
        if (!polylineActiva) return;

        polylineActiva.setPath([]);
        this.redibujarMarcadoresRuta();
    },

    limpiarRuta: function () {
        if (this.polylineJesus) this.polylineJesus.setPath([]);
        if (this.polylineVirgen) this.polylineVirgen.setPath([]);
        this.redibujarMarcadoresRuta();
    },

    limpiarCheckpoints: function () {
        this.checkpoints = [];
        this.redibujarCheckpoints();
    },

    getGeoJson: function () {
        const obtenerCoordenadas = (polyline) => {
            const path = polyline.getPath();
            const coordinates = [];

            for (let i = 0; i < path.getLength(); i++) {
                const point = path.getAt(i);
                coordinates.push([point.lng(), point.lat()]);
            }

            return coordinates;
        };

        const coordsJesus = this.polylineJesus ? obtenerCoordenadas(this.polylineJesus) : [];
        const coordsVirgen = this.polylineVirgen ? obtenerCoordenadas(this.polylineVirgen) : [];

        const features = [];

        if (coordsJesus.length > 0) {
            features.push({
                type: "Feature",
                properties: {
                    ruta: "jesus",
                    nombre: "Jesus",
                    color: "#8E24AA"
                },
                geometry: {
                    type: "LineString",
                    coordinates: coordsJesus
                }
            });
        }

        if (coordsVirgen.length > 0) {
            features.push({
                type: "Feature",
                properties: {
                    ruta: "virgen",
                    nombre: "Virgen",
                    color: "#FFD600"
                },
                geometry: {
                    type: "LineString",
                    coordinates: coordsVirgen
                }
            });
        }

        return {
            type: "FeatureCollection",
            features: features
        };
    },

    getCheckpoints: function () {
        return this.checkpoints.map((p, index) => ({
            Id: p.Id || 0,
            RecorridoId: p.RecorridoId || 0,
            Latitud: p.Latitud,
            Longitud: p.Longitud,
            Orden: index + 1,
            Referencia: p.Referencia || "",
            Grupo: p.Grupo || "",
            Tipo: this.normalizarTipo(p.Tipo || "Cargador")
        }));
    },

    dispose: function (limpiarElemento = true) {
        if (this.infoWindowCheckpoint) {
            this.infoWindowCheckpoint.close();
            this.infoWindowCheckpoint = null;
        }

        if (this.mapClickListener) {
            google.maps.event.removeListener(this.mapClickListener);
            this.mapClickListener = null;
        }

        this.markers.forEach(m => {
            google.maps.event.clearInstanceListeners(m);
            m.setMap(null);
        });
        this.markers = [];

        this.checkpointMarkers.forEach(m => {
            google.maps.event.clearInstanceListeners(m);
            m.setMap(null);
        });
        this.checkpointMarkers = [];

        if (this.polylineJesus) {
            google.maps.event.clearInstanceListeners(this.polylineJesus);
            this.polylineJesus.setMap(null);
            this.polylineJesus = null;
        }

        if (this.polylineVirgen) {
            google.maps.event.clearInstanceListeners(this.polylineVirgen);
            this.polylineVirgen.setMap(null);
            this.polylineVirgen = null;
        }

        this.checkpoints = [];
        this.modoCheckpoint = false;
        this.rutaActiva = "jesus";

        if (limpiarElemento && this.map && this.map.getDiv) {
            const div = this.map.getDiv();
            if (div) {
                div.innerHTML = "";
            }
        }

        this.map = null;
    }
};