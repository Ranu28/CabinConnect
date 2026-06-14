namespace CabinConnect.Api.Models;

public sealed record PlaceHoldRequest(string CabinId, string CheckIn, string CheckOut);
