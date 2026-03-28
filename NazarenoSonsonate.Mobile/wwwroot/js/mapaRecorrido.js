window.mapaRecorrido = {
    map: null,
    dataLayer: null,
    marcadorTiempoReal: null,
    marcadorUsuario: null,
    infoTiempoReal: null,

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
    },

    drawGeoJson: function (geoJsonText) {
        if (!this.map || !this.dataLayer || !geoJsonText) return;

        this.dataLayer.forEach(feature => {
            this.dataLayer.remove(feature);
        });

        const data = JSON.parse(geoJsonText);
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