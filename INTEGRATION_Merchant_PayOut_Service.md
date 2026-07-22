# Merchant Pay Out Service

Cette interface permet à un marchand d'envoyer, à partir de son compte, de l'argent électronique vers un numéro de téléphone disposant d'un compte Mobile Money.

## Endpoint

* **URL** : `http://ip:port/api/rest/v1/merchantPayOutService`
* **Méthode** : `POST`
* **Format** : `JSON`

---

## Headers

| Champ         | Description          | Exemple                   | Obligatoire |
| ------------- | -------------------- | ------------------------- | ----------- |
| Authorization | Token d'autorisation | `Bearer xxxxxxxxxxxxxxxx` | Oui         |

---

## Corps de la requête

| Champ       | Description                                                    | Exemple                | Obligatoire |
| ----------- | -------------------------------------------------------------- | ---------------------- | ----------- |
| merchant    | Code Marchand FlexPay                                          | `ZANDO`                | Oui         |
| type        | Type de transaction : `1` = Mobile Money, `2` = Carte bancaire | `1`                    | Oui         |
| reference   | Référence de la transaction                                    | `MM0000159`            | Oui         |
| phone       | Numéro de téléphone du bénéficiaire                            | `243891234567`         | Oui         |
| amount      | Montant de la transaction                                      | `100`                  | Oui         |
| currency    | Devise : `CDF` ou `USD`                                        | `CDF`                  | Oui         |
| callbackUrl | URL de retour pour recevoir le résultat de la transaction      | `https://abcd.efgh.cd` | Oui         |

---

## Exemple de requête

```json
{
  "merchant": "ZANDO",
  "type": "1",
  "phone": "243891234567",
  "reference": "MLOPN5472458",
  "amount": "100",
  "currency": "CDF",
  "callbackUrl": "https://abcd.efgh.cd"
}
```

---

## Réponse immédiate

### Champs de la réponse

| Champ       | Description                                     | Exemple                            |
| ----------- | ----------------------------------------------- | ---------------------------------- |
| code        | `0` : requête envoyée avec succès, `1` : erreur | `0`                                |
| message     | Description du résultat                         | `Transaction envoyée avec succès.` |
| orderNumber | Identifiant généré par FlexPay                  | `9bsTX7qXdpQe243815877848`         |

### Exemple de réponse

```json
{
  "code": "0",
  "message": "Transaction envoyée avec succès.",
  "orderNumber": "SQeCGunXEGnr243815877848"
}
```

---

## Callback : Résultat de la transaction

FlexPay envoie une requête HTTP vers l'URL définie dans `callbackUrl` afin de notifier le résultat de la transaction.

### Champs retournés

| Champ              | Description                           | Exemple                    |
| ------------------ | ------------------------------------- | -------------------------- |
| code               | `0` : succès, autre : échec           | `0`                        |
| reference          | Référence envoyée dans la requête     | `TESTZANDO21003000002`     |
| provider_reference | Référence de l'opérateur Mobile Money | `7KI81020PHS`              |
| orderNumber        | Identifiant généré par FlexPay        | `9bsTX7qXdpQe243815877848` |
| amount             | Montant défini par le marchand        | `0.5`                      |
| amountCustomer     | Montant total avec commission         | `0.51`                     |
| phone              | Wallet utilisé par le client          | `243815877848`             |
| currency           | Devise                                | `USD`                      |
| createdAt          | Date et heure de la transaction       | `24-03-2022 09:57:15`      |
| channel            | Canal de paiement                     | `MPESA`                    |

### Exemple de callback

```json
{
  "code": "1",
  "reference": "TESTZANDO21003000002",
  "amount": "0.5",
  "amountCustomer": "0.51",
  "phone": "243815877848",
  "currency": "USD",
  "createdAt": "24-03-2022 09:57:15",
  "channel": "mpesa",
  "orderNumber": "SQeCGunXEGnr243815877848",
  "provider_reference": ""
}
```

