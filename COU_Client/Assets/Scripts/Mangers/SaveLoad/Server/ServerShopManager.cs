using System;
using System.Collections.Generic;
using UnityEngine;

public class ServerShopManager : MonoSingleton<ServerShopManager>
{
    private bool isRequesting;

    // onComplete(성공 여부, 실패 메시지, 새로 받은 장비 목록)
    public void Purchase(int productId, Action<bool, string, List<OwnedItem>> onComplete)
    {
        if (isRequesting)
        {
            onComplete?.Invoke(false, "요청을 처리하고 있습니다. 잠시만 기다려 주세요.", null);
            return;
        }

        isRequesting = true;
        StartCoroutine(ShopApi.Purchase(productId, result =>
        {
            isRequesting = false;

            if (!result.IsSuccess)
            {
                onComplete?.Invoke(false, result.ToUserMessage("구매하지 못했습니다."), null);
                return;
            }

            var added = PlayerInventory.Instance.ApplyPurchase(result.Data);
            onComplete?.Invoke(true, null, added);
        }));
    }
}
