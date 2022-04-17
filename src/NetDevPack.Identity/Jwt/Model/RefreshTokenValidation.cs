namespace NetDevPack.Identity.Jwt.Model;

public class RefreshTokenValidation
{
    public RefreshTokenValidation(bool isValid, string userId = null, string reason = null)
    {
        IsValid = isValid;
        UserId = userId;
        Reason = reason;
    }

    public bool IsValid { get; set; }
    public string UserId { get; set; }
    public string Reason { get; }

    public static implicit operator bool(RefreshTokenValidation validation) => validation.IsValid;
}