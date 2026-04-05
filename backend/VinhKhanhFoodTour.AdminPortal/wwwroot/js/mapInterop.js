window.leafletMapInterop = {
    map: null,
    marker: null,
    dotNetRef: null,

    initMap: function (elementId, dotNetObj, initialLat, initialLng) {
        // District 4 HCMC center / Vinh Khanh area if no coordinates provided
        if (!initialLat || !initialLng || initialLat === 0 || initialLng === 0) {
            initialLat = 10.7600;
            initialLng = 106.6961;
        }

        this.dotNetRef = dotNetObj;

        // If map is already initialized, update view and marker
        if (this.map) {
            this.map.setView([initialLat, initialLng], 15);
            if (this.marker) {
                this.marker.setLatLng([initialLat, initialLng]);
            }
            return;
        }

        // Initialize map
        this.map = L.map(elementId).setView([initialLat, initialLng], 15);

        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
            attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
        }).addTo(this.map);

        // Add draggable marker
        this.marker = L.marker([initialLat, initialLng], { draggable: true }).addTo(this.map);

        // Update coordinates when marker is dragged
        this.marker.on('dragend', function (event) {
            var marker = event.target;
            var position = marker.getLatLng();
            window.leafletMapInterop.dotNetRef.invokeMethodAsync('UpdateCoordinates', position.lat, position.lng);
        });

        // Move marker and update coordinates when map is clicked
        this.map.on('click', function (e) {
            var lat = e.latlng.lat;
            var lng = e.latlng.lng;
            window.leafletMapInterop.marker.setLatLng(e.latlng);
            window.leafletMapInterop.dotNetRef.invokeMethodAsync('UpdateCoordinates', lat, lng);
        });
        
        // Force leafet to recalculate its map size to prevent gray areas
        setTimeout(() => {
            if(window.leafletMapInterop.map) {
                window.leafletMapInterop.map.invalidateSize();
            }
        }, 100);
    },

    destroyMap: function() {
        if (this.map) {
            this.map.remove();
            this.map = null;
            this.marker = null;
            this.dotNetRef = null;
        }
    }
};
