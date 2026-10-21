using System;
using UnityEngine;
using System.Collections;

public static class ShopApi
{
    public const string PurchasePath = "/api/shop/purchase";

    public static IEnumerator Purchase(int productId, Action<ApiResult<PurchaseResponse>> onComplete)
        => ApiClient.Post(PurchasePath, new PurchaseRequest { productId = productId }, onComplete);
}