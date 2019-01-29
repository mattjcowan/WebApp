namespace WebApp
{
    public class ApiResponse<TResult>
    {
        public virtual TResult Result { get; set; }   
    }
}