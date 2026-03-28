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

    init: function (elementId, lat, lng, zoom, geoJsonText, checkpointsJson) {
        const element = document.getElementById(elementId);
        if (!element) return;

        this.map = new google.maps.Map(element, {
            center: { lat: lat, lng: lng },
            zoom: zoom,
            mapTypeId: 'roadmap',
            streetViewControl: false,
            fullscreenControl: false,
            mapTypeControl: false
        });

        this.polyline = new google.maps.Polyline({
            map: this.map,
            path: [],
            strokeColor: '#6A1B9A',
            strokeOpacity: 1,
            strokeWeight: 4,
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
                const referencia = prompt("Referencia del checkpoint:", "");
                if (referencia === null) {
                    this.modoCheckpoint = false;
                    return;
                }

                const grupo = prompt("¿Qué grupo va ahí?", "");
                if (grupo === null) {
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
                    Grupo: grupo
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
                    Grupo: p.Grupo ?? p.grupo ?? ""
                }))
                : [];

            this.redibujarCheckpoints();
        } catch (e) {
            console.error("Error cargando checkpoints:", e);
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

    redibujarCheckpoints: function () {
        this.checkpointMarkers.forEach(m => m.setMap(null));
        this.checkpointMarkers = [];

        const purplePin = {
            path: "M12 2C8.13 2 5 5.13 5 9c0 5.25 7 13 7 13s7-7.75 7-13c0-3.87-3.13-7-7-7z",
            fillColor: "#6A1B9A",
            fillOpacity: 1,
            strokeColor: "#ffffff",
            strokeWeight: 2,
            scale: 2,
            anchor: new google.maps.Point(12, 22)
        };

        this.checkpoints.forEach((punto, index) => {
            punto.Orden = index + 1;

            const marker = new google.maps.Marker({
                position: { lat: punto.Latitud, lng: punto.Longitud },
                map: this.map,
                draggable: true,
                title: punto.Grupo || punto.Referencia || `Checkpoint ${index + 1}`,
                icon: purplePin
            });

            const construirContenido = () => `
                <div>
                    <strong>${punto.Grupo || "Grupo sin definir"}</strong>
                    ${punto.Referencia ? `<br/>${punto.Referencia}` : ""}
                </div>`;

            const info = new google.maps.InfoWindow({
                content: construirContenido()
            });

            marker.addListener("click", () => {
                const nuevaReferencia = prompt("Editar referencia:", punto.Referencia || "");
                if (nuevaReferencia === null) return;

                const nuevoGrupo = prompt("Editar grupo:", punto.Grupo || "");
                if (nuevoGrupo === null) return;

                punto.Referencia = nuevaReferencia;
                punto.Grupo = nuevoGrupo;
                marker.setTitle(punto.Grupo || punto.Referencia || `Checkpoint ${index + 1}`);
                info.setContent(construirContenido());
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
            Grupo: p.Grupo || ""
        }));
    }
};