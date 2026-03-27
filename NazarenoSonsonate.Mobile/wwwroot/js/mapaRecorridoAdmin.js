window.mapaRecorridoAdmin = {
    map: null,
    drawnItems: null,
    drawControl: null,

    init: function (elementId, lat, lng, zoom) {
        if (this.map) {
            this.map.remove();
            this.map = null;
        }

        this.map = L.map(elementId).setView([lat, lng], zoom);

        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
            attribution: '© OpenStreetMap contributors'
        }).addTo(this.map);

        this.drawnItems = new L.FeatureGroup();
        this.map.addLayer(this.drawnItems);

        this.drawControl = new L.Control.Draw({
            edit: {
                featureGroup: this.drawnItems,
                remove: true
            },
            draw: {
                polygon: false,
                rectangle: false,
                circle: false,
                circlemarker: false,
                marker: true,
                polyline: {
                    shapeOptions: {
                        color: 'green',
                        weight: 5
                    }
                }
            }
        });

        this.map.addControl(this.drawControl);

        this.map.on(L.Draw.Event.CREATED, (e) => {
            this.drawnItems.addLayer(e.layer);
        });
    },

    getGeoJson: function () {
        if (!this.drawnItems) return null;
        return this.drawnItems.toGeoJSON();
    }
};