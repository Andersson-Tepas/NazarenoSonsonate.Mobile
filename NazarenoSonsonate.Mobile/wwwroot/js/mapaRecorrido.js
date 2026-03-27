window.mapaRecorrido = {
    map: null,
    routeLayer: null,

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
    }
};