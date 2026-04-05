select f."Id" as fatura_id, f."FaturaNo", f."CariKartId", c."Id" as cari_id, c."Unvan", c."AdiSoyadi", c."VTCK_No", c."Gsm", c."Telefon", c."HksSifatId", c."HksIsletmeTuruId", c."HksHalIciIsyeriId", c."HksIlId", c."HksIlceId", c."HksBeldeId", c."FaturaTipi"
from "Faturalar" f
left join "CariKartlar" c on c."Id" = f."CariKartId"
where f."Id" = '475e9d01-e345-467e-b013-0b1a5ee19828';
