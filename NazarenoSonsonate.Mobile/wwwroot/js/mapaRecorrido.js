window.mapaRecorrido = {
    map: null,
    routeLayer: null,
    marcadorTiempoReal: null,
    marcadorUsuario: null,

    init: function (elementId, lat, lng, zoom) {
        if (this.map) {
            this.map.remove();
            this.map = null;
        }

        this.map = L.map(elementId).setView([lat, lng], zoom);

        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
            attribution: '© OpenStreetMap contributors'
        }).addTo(this.map);

        this.routeLayer = L.layerGroup().addTo(this.map);
        this.marcadorTiempoReal = null;
        this.marcadorUsuario = null;
    },

    drawGeoJson: function (geoJsonText) {
        if (!this.map || !geoJsonText) return;

        this.routeLayer.clearLayers();

        const data = JSON.parse(geoJsonText);

        const layer = L.geoJSON(data, {
            style: {
                color: 'green',
                weight: 5
            }
        }).addTo(this.routeLayer);

        if (layer.getBounds && layer.getBounds().isValid()) {
            this.map.fitBounds(layer.getBounds(), { padding: [20, 20] });
        }
    },

    actualizarMarcadorTiempoReal: function (lat, lng, grupoActual, mensaje, fechaHora) {
        if (!this.map) return;

        if (!this.marcadorTiempoReal) {
            this.marcadorTiempoReal = L.marker([lat, lng]).addTo(this.map);
        } else {
            this.marcadorTiempoReal.setLatLng([lat, lng]);
        }

        this.marcadorTiempoReal.bindPopup(
            `<b>${grupoActual || 'Procesión'}</b><br>${mensaje || ''}<br>${fechaHora || ''}`
        );
    },

    mostrarMiUbicacion: function (lat, lng) {
        if (!this.map) return;

        if (!this.marcadorUsuario) {
            this.marcadorUsuario = L.marker([lat, lng]).addTo(this.map);
        } else {
            this.marcadorUsuario.setLatLng([lat, lng]);
        }

        this.marcadorUsuario.bindPopup("<b>Mi ubicación</b>");
        this.map.panTo([lat, lng]);
    }
};