window.mapaRecorrido = {
    map: null,
    routeLayer: null,
    markersLayer: null,

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
        this.markersLayer = L.layerGroup().addTo(this.map);
    },

    drawRoute: function (points) {
        if (!this.map || !points || points.length === 0) return;

        this.routeLayer.clearLayers();
        this.markersLayer.clearLayers();

        const latlngs = points.map(p => [p.latitud, p.longitud]);

        const polyline = L.polyline(latlngs, {
            color: 'green',
            weight: 5
        }).addTo(this.routeLayer);

        points.forEach(p => {
            L.marker([p.latitud, p.longitud])
                .addTo(this.markersLayer)
                .bindPopup(`Punto ${p.orden}: ${p.referencia}`);
        });

        this.map.fitBounds(polyline.getBounds(), { padding: [20, 20] });
    }
};