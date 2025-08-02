# Ecv2DotNet

Ecv2DotNet is a .NET library that provides a simple way to verify the integrity of payloads that are signed/encrypted
using the Google ecv2 protocol. Currently it supports `ECv2SigningOnly` protocol, which is used for signing payloads without encryption. 
Intention is to eventually support [`ECv2`](https://developers.google.com/pay/api/android/guides/resources/payment-data-cryptography) protocol as well.

Below is an example of a payload that is signed using the `ECv2SigningOnly` protocol. This has come from the Google Wallet callback API.

```
{
  "signature": "MEUCIQCJi26vl+ak17dsHDbZZnRZxm51duUAPiYLwOIr9rVvAAIgGUfR18gpKTq1+Msav0vPrWvC6x9dDRwWFX/b85+jE1k\u003d",
  "intermediateSigningKey": {
    "signedKey": "{\"keyValue\":\"MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAEVsEtOdPMaE+DJDzuCJaO7EJXaHor4Kyklp411iwfBa+5TmdbiEWUXzewA79H0PXjdyRMhKBY99+sh056JB75LQ\\u003d\\u003d\",\"keyExpiration\":\"1754778096000\"}",
    "signatures": [
      "MEUCIC29Ju3bt9kklbbA9QAJZW0hh2zecbHDzGo4hF1zRi1zAiEA6e201l1TEl85Row6XHybfDoewIKC4vYpnrlmUT9WbrE\u003d"
    ]
  },
  "protocolVersion": "ECv2SigningOnly",
  "signedMessage": "{\"classId\":\"1388000000022025937.LOYALTY_CLASS_dada6069-0799-44ec-a38d-c482484902e1\",\"objectId\":\"3388000000022025937.LOYALTY_OBJECT_xxxxxxxxxxxxx\",\"eventType\":\"save\",\"expTimeMillis\":1754114831806,\"count\":1,\"nonce\":\"40a8e5af-5b7f-4ea4-b152-63d96858550e\"}"
}
```

## Installation

```bash
dotnet add package Ecv2DotNet
```

## Basic Usage

### 

### 1. Dependency Injection Setup
