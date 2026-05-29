namespace OrderApi
{
    /// <summary>
    /// Dữ liệu dự báo thời tiết
    /// </summary>
    public class WeatherForecast
    {
        /// <summary>
        /// Ngày dự báo
        /// </summary>
        public DateOnly Date { get; set; }

        /// <summary>
        /// Nhiệt độ theo độ Celsius
        /// </summary>
        public int TemperatureC { get; set; }

        /// <summary>
        /// Nhiệt độ theo độ Fahrenheit (tính toán từ Celsius)
        /// </summary>
        public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);

        /// <summary>
        /// Mô tả ngắn về thời tiết
        /// </summary>
        public string? Summary { get; set; }
    }
}
