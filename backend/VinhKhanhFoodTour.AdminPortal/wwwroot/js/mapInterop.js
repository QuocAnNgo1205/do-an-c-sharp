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
    },

    initOverviewMap: function (elementId, markersData) {
        if (this.map) {
            this.map.remove();
        }

        // Center initially at District 4
        this.map = L.map(elementId).setView([10.7600, 106.6961], 15);

        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
            attribution: '&copy; OpenStreetMap contributors'
        }).addTo(this.map);

        setTimeout(() => {
            if(window.leafletMapInterop.map) {
                window.leafletMapInterop.map.invalidateSize();
            }
        }, 200);

        if (!markersData || markersData.length === 0) {
            return;
        }

        var bounds = [];
        
        // Custom icons
        var greenIcon = new L.Icon({
            iconUrl: 'https://raw.githubusercontent.com/pointhi/leaflet-color-markers/master/img/marker-icon-2x-green.png',
            shadowUrl: 'https://cdnjs.cloudflare.com/ajax/libs/leaflet/0.7.7/images/marker-shadow.png',
            iconSize: [25, 41], iconAnchor: [12, 41], popupAnchor: [1, -34], shadowSize: [41, 41]
        });
        
        var orangeIcon = new L.Icon({
            iconUrl: 'https://raw.githubusercontent.com/pointhi/leaflet-color-markers/master/img/marker-icon-2x-orange.png',
            shadowUrl: 'https://cdnjs.cloudflare.com/ajax/libs/leaflet/0.7.7/images/marker-shadow.png',
            iconSize: [25, 41], iconAnchor: [12, 41], popupAnchor: [1, -34], shadowSize: [41, 41]
        });

        var redIcon = new L.Icon({
            iconUrl: 'https://raw.githubusercontent.com/pointhi/leaflet-color-markers/master/img/marker-icon-2x-red.png',
            shadowUrl: 'https://cdnjs.cloudflare.com/ajax/libs/leaflet/0.7.7/images/marker-shadow.png',
            iconSize: [25, 41], iconAnchor: [12, 41], popupAnchor: [1, -34], shadowSize: [41, 41]
        });

        markersData.forEach(m => {
            var mIcon = m.status === 1 ? greenIcon : (m.status === 0 ? orangeIcon : redIcon);
            var statusText = m.status === 1 ? "✅ Đã duyệt" : (m.status === 0 ? "⏳ Đang chờ duyệt" : "❌ Từ chối");
            
            var marker = L.marker([m.latitude, m.longitude], {icon: mIcon}).addTo(this.map);
            marker.bindPopup(`
                <div style="min-width: 150px">
                    <h3 style="font-weight: bold; margin-bottom: 5px; font-size: 14px">${m.name}</h3>
                    <p style="margin: 0; font-size: 12px">Trạng thái: <b>${statusText}</b></p>
                    <a href="/poi/${m.id}" style="display: block; margin-top: 8px; color: #0066cc; text-decoration: underline;">👉 Xem chi tiết</a>
                </div>
            `);
            bounds.push([m.latitude, m.longitude]);
        });

        // Fit map constraints to markers
        if (bounds.length > 0) {
            this.map.fitBounds(bounds, { padding: [30, 30] });
        }
    },

    renderHeatmap: function (elementId, markersData) {
        if (this.map) {
            this.map.remove();
        }

        // Center initially at District 4
        this.map = L.map(elementId).setView([10.7600, 106.6961], 15);

        // Sử dụng nền tối (Dark mode) hoặc nền mặc định để Heatmap nổi bật hơn
        L.tileLayer('https://{s}.basemaps.cartocdn.com/dark_all/{z}/{x}/{y}{r}.png', {
            attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OSM</a> contributors &copy; <a href="https://carto.com/attributions">CARTO</a>',
            subdomains: 'abcd',
            maxZoom: 19
        }).addTo(this.map);

        setTimeout(() => {
            if(window.leafletMapInterop.map) {
                window.leafletMapInterop.map.invalidateSize();
            }
        }, 200);

        if (!markersData || markersData.length === 0) {
            return;
        }

        var bounds = [];
        var heatPoints = [];
        
        // Find max listen count to normalize radius
        var maxListens = 1;
        markersData.forEach(m => {
            if (m.listenCount > maxListens) maxListens = m.listenCount;
        });

        markersData.forEach(m => {
            if (m.listenCount > 0) {
                // Thêm độ đậm của điểm nhiệt (intensity)
                var intensity = m.listenCount / maxListens;
                heatPoints.push([m.latitude, m.longitude, intensity]);
            }
            bounds.push([m.latitude, m.longitude]);
        });

        // Add heat layer
        if (heatPoints.length > 0) {
            L.heatLayer(heatPoints, {
                radius: 35,
                blur: 25,
                maxZoom: 15,
                max: 1.0,
                gradient: {
                    0.2: 'blue', 
                    0.4: 'cyan', 
                    0.6: 'lime', 
                    0.8: 'yellow', 
                    1.0: 'red'
                }
            }).addTo(this.map);
        }

        // Fit map constraints to markers
        if (bounds.length > 0) {
            this.map.fitBounds(bounds, { padding: [30, 30] });
        }
    }
};
