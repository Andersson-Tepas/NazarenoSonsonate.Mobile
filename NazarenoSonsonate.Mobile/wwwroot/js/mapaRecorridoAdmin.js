window.mapaRecorridoAdmin = {
    map: null,
    polyline: null,
    markers: [],
    checkpointMarkers: [],
    checkpoints: [],
    modoCheckpoint: false,

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

    init: function (elementId, lat, lng, zoom, geoJsonText, checkpointsJson) {
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

        this.polyline = new google.maps.Polyline({
            map: this.map,
            path: [],
            strokeColor: "#8E24AA",
            strokeOpacity: 1,
            strokeWeight: 5,
            editable: true
        });

        this.markers = [];
        this.checkpointMarkers = [];
        this.checkpoints = [];
        this.modoCheckpoint = false;

        if (geoJsonText) {
            this.cargarGeoJson(geoJsonText);
        }

        if (checkpointsJson) {
            this.cargarCheckpoints(checkpointsJson);
        }

        this.map.addListener("click", (event) => {
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

            this.polyline.getPath().push(event.latLng);
            this.redibujarMarcadoresRuta();
        });
    },

    activarModoCheckpoint: function () {
        this.modoCheckpoint = true;
    },

    cargarGeoJson: function (geoJsonText) {
        try {
            const data = JSON.parse(geoJsonText);
            let coordinates = [];

            if (data.type === "FeatureCollection" && data.features?.length > 0) {
                const feature = data.features.find(f => f.geometry?.type === "LineString");
                if (feature) {
                    coordinates = feature.geometry.coordinates || [];
                }
            } else if (data.type === "Feature" && data.geometry?.type === "LineString") {
                coordinates = data.geometry.coordinates || [];
            } else if (data.type === "LineString") {
                coordinates = data.coordinates || [];
            }

            const path = this.polyline.getPath();
            path.clear();

            const bounds = new google.maps.LatLngBounds();

            coordinates.forEach(coord => {
                const lng = coord[0];
                const lat = coord[1];
                const latLng = new google.maps.LatLng(lat, lng);
                path.push(latLng);
                bounds.extend(latLng);
            });

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
            const puntos = JSON.parse(checkpointsJson);

            this.checkpoints = Array.isArray(puntos)
                ? puntos.map(p => ({
                    Id: p.Id ?? p.id ?? 0,
                    RecorridoId: p.RecorridoId ?? p.recorridoId ?? 0,
                    Latitud: p.Latitud ?? p.latitud ?? 0,
                    Longitud: p.Longitud ?? p.longitud ?? 0,
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
        this.markers.forEach(m => m.setMap(null));
        this.markers = [];

        const path = this.polyline.getPath();

        for (let i = 0; i < path.getLength(); i++) {
            const point = path.getAt(i);

            const marker = new google.maps.Marker({
                position: point,
                map: this.map,
                label: `${i + 1}`,
                icon: {
                    url: "http://maps.google.com/mapfiles/ms/icons/yellow-dot.png"
                }
            });

            marker.addListener("click", () => {
                const pathActual = this.polyline.getPath();
                pathActual.removeAt(i);
                this.redibujarMarcadoresRuta();
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
        this.checkpointMarkers.forEach(m => m.setMap(null));
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

            const info = new google.maps.InfoWindow({
                content: construirContenido()
            });

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
                info.setContent(construirContenido());
            });

            marker.addListener("dblclick", () => {
                info.open(this.map, marker);
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
        if (!this.polyline) return;

        const path = this.polyline.getPath();
        const length = path.getLength();

        if (length > 0) {
            path.removeAt(length - 1);
            this.redibujarMarcadoresRuta();
        }
    },

    limpiarRuta: function () {
        if (!this.polyline) return;
        this.polyline.setPath([]);
        this.redibujarMarcadoresRuta();
    },

    limpiarCheckpoints: function () {
        this.checkpoints = [];
        this.redibujarCheckpoints();
    },

    getGeoJson: function () {
        if (!this.polyline) return null;

        const path = this.polyline.getPath();
        const coordinates = [];

        for (let i = 0; i < path.getLength(); i++) {
            const point = path.getAt(i);
            coordinates.push([point.lng(), point.lat()]);
        }

        return {
            type: "FeatureCollection",
            features: [
                {
                    type: "Feature",
                    properties: {},
                    geometry: {
                        type: "LineString",
                        coordinates: coordinates
                    }
                }
            ]
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
    }
};