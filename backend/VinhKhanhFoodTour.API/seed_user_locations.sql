-- Script này để tạo dữ liệu giả cho Bản đồ nhiệt (Heatmap) vị trí người dùng
-- Nó sẽ tạo các điểm xung quanh khu vực đường Vĩnh Khánh, Quận 4, TP.HCM

-- Chèn dữ liệu vào bảng UserLocationLogs
-- Lưu ý: Nếu bạn chưa chạy Migration, hãy chạy 'dotnet ef migrations add AddUserLocationLogs' và 'dotnet ef database update' trước.

DECLARE @BaseLat FLOAT = 10.7590;
DECLARE @BaseLng FLOAT = 106.6961;
DECLARE @i INT = 0;

WHILE @i < 200
BEGIN
    -- Tạo tọa độ ngẫu nhiên xung quanh khu vực Vĩnh Khánh (+/- 0.005 độ)
    INSERT INTO [dbo].[UserLocationLogs] ([DeviceId], [Latitude], [Longitude], [Timestamp])
    VALUES (
        'DummyDevice-' + CAST((@i % 10) AS VARCHAR), 
        @BaseLat + (RAND() - 0.5) * 0.01, 
        @BaseLng + (RAND() - 0.5) * 0.01, 
        DATEADD(HOUR, -RAND() * 24, GETUTCDATE())
    );
    SET @i = @i + 1;
END

-- Thêm một vài cụm đậm đặc hơn tại các POI chính
INSERT INTO [dbo].[UserLocationLogs] ([DeviceId], [Latitude], [Longitude], [Timestamp])
SELECT TOP 50 'ClusterDevice', Latitude + (RAND()-0.5)*0.001, Longitude + (RAND()-0.5)*0.001, GETUTCDATE()
FROM [dbo].[Pois] 
WHERE [Status] = 'Approved';
