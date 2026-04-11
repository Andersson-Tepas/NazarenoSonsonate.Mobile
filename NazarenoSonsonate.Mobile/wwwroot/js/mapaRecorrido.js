window.mapaRecorrido = {
    map: null,
    dataLayer: null,
    marcadorTiempoReal: null,
    marcadorUsuario: null,
    infoTiempoReal: null,
    puntoMarkers: [],

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
            mapTypeId: 'roadmap',
            streetViewControl: false,
            fullscreenControl: false,
            mapTypeControl: false
        });

        this.dataLayer = new google.maps.Data({ map: this.map });
        this.marcadorTiempoReal = null;
        this.marcadorUsuario = null;
        this.infoTiempoReal = new google.maps.InfoWindow();
        this.puntoMarkers = [];
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
            strokeColor: '#6A1B9A',
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
        if (!this.map || !puntosJson) return;

        this.limpiarPuntosRuta();

        try {
            const puntos = typeof puntosJson === "string"
                ? JSON.parse(puntosJson)
                : puntosJson;

            if (!Array.isArray(puntos)) return;

            const bounds = new google.maps.LatLngBounds();
            const iconoGrupo = this.crearIconoGrupo();

            puntos.forEach((punto, index) => {
                const lat = punto.Latitud ?? punto.latitud;
                const lng = punto.Longitud ?? punto.longitud;
                const grupo = (punto.Grupo ?? punto.grupo ?? `${index + 1}`).toString().trim();
                const referencia = punto.Referencia ?? punto.referencia ?? "";

                if (lat == null || lng == null) return;

                const marker = new google.maps.Marker({
                    position: { lat: lat, lng: lng },
                    map: this.map,
                    icon: iconoGrupo,
                    title: grupo || referencia || `Punto ${index + 1}`,
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
                bounds.extend(marker.getPosition());
            });

            if (!bounds.isEmpty()) {
                this.map.fitBounds(bounds);
            }
        } catch (error) {
            console.error("Error al dibujar puntos de ruta:", error);
        }
    },

    actualizarMarcadorTiempoReal: function (lat, lng, grupoActual, mensaje, fechaHora) {
        if (!this.map) return;

        const position = { lat: lat, lng: lng };

        const iconoProcesion = {
            path: google.maps.SymbolPath.CIRCLE,
            scale: 10,
            fillColor: '#6A1B9A',
            fillOpacity: 1,
            strokeColor: '#ffffff',
            strokeWeight: 3
        };

        if (!this.marcadorTiempoReal) {
            this.marcadorTiempoReal = new google.maps.Marker({
                position: position,
                map: this.map,
                title: grupoActual || 'Procesión',
                icon: iconoProcesion
            });

            this.marcadorTiempoReal.addListener('click', () => {
                this.infoTiempoReal.open(this.map, this.marcadorTiempoReal);
            });
        } else {
            this.marcadorTiempoReal.setPosition(position);
            this.marcadorTiempoReal.setTitle(grupoActual || 'Procesión');
            this.marcadorTiempoReal.setIcon(iconoProcesion);
        }

        this.infoTiempoReal.setContent(`<div>
            <strong>${grupoActual || 'Procesión'}</strong><br/>
            ${mensaje || ''}
        </div>`);
    },

    mostrarMiUbicacion: function (lat, lng, centrar = false) {
        if (!this.map) return;

        const position = { lat: lat, lng: lng };

        const iconoUsuario = {
            path: google.maps.SymbolPath.CIRCLE,
            scale: 9,
            fillColor: '#1E88E5',
            fillOpacity: 1,
            strokeColor: '#ffffff',
            strokeWeight: 3
        };

        if (!this.marcadorUsuario) {
            this.marcadorUsuario = new google.maps.Marker({
                position: position,
                map: this.map,
                title: 'Mi ubicación',
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