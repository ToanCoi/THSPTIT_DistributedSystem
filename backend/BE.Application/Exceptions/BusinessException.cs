namespace BE.Application.Exceptions
{
    /// <summary>
    /// Exception cho các lỗi nghiệp vụ
    /// </summary>
    public class BusinessException : Exception
    {
        /// <summary>
        /// Mã HTTP status code trả về
        /// </summary>
        public int StatusCode { get; }

        /// <summary>
        /// Khởi tạo BusinessException với message và status code mặc định 422
        /// </summary>
        /// <param name="message">Thông báo lỗi</param>
        public BusinessException(string message) : base(message)
        {
            StatusCode = 422;
        }

        /// <summary>
        /// Khởi tạo BusinessException với message và status code
        /// </summary>
        /// <param name="message">Thông báo lỗi</param>
        /// <param name="statusCode">Mã HTTP status code</param>
        public BusinessException(string message, int statusCode) : base(message)
        {
            StatusCode = statusCode;
        }
    }
}
