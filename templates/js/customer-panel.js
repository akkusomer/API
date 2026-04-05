(function () {
    const sectionMeta = {
        dashboard: {
            title: 'Genel Bakis',
            subtitle: 'Hos geldin, sistemdeki son durum burada.'
        },
        stocks: {
            title: 'Stok Yonetimi',
            subtitle: 'Stok kartlarini goruntuleyin, filtreleyin ve yonetin.'
        },
        cariler: {
            title: 'Cari Kartlar',
            subtitle: 'Cari musterilerinizi ve tedarikcilerinizi yonetin.'
        },
        settings: {
            title: 'Ayarlar',
            subtitle: 'Sirkete ozel birimler ve cari tipleri burada listelenir.'
        }
    };

    const state = {
        stocks: [],
        caris: [],
        units: [],
        cariTypes: [],
        activeCariTypeId: ''
    };

    function byId(id) {
        return document.getElementById(id);
    }

    function readHashSection() {
        const rawHash = window.location.hash.replace(/^#/, '').trim().toLowerCase();
        return sectionMeta[rawHash] ? rawHash : 'dashboard';
    }

    function setPageMeta(sectionName) {
        const meta = sectionMeta[sectionName] || sectionMeta.dashboard;
        const pageTitle = byId('page-title');
        const pageSubtitle = byId('page-subtitle');

        if (pageTitle) {
            pageTitle.textContent = meta.title;
        }

        if (pageSubtitle) {
            pageSubtitle.textContent = meta.subtitle;
        }
    }

    function setActiveMenu(sectionName) {
        const menuMap = {
            dashboard: 'btn-dashboard',
            stocks: 'btn-stocks',
            cariler: 'btn-cariler',
            settings: 'btn-settings'
        };

        Object.values(menuMap).forEach(id => {
            byId(id)?.classList.remove('active');
        });

        byId(menuMap[sectionName])?.classList.add('active');
    }

    function showSection(sectionName, syncHash = true) {
        const safeSection = sectionMeta[sectionName] ? sectionName : 'dashboard';

        document.querySelectorAll('.page-section').forEach(section => {
            section.classList.toggle('active', section.id === `section-${safeSection}`);
        });

        setActiveMenu(safeSection);
        setPageMeta(safeSection);

        if (syncHash && window.location.hash !== `#${safeSection}`) {
            window.location.hash = safeSection;
        }

        if (safeSection === 'settings') {
            void loadSettings();
        }
    }

    function escapeText(value, fallback = '-') {
        return apiClient.safeText(value, fallback);
    }

    function resolveCariTypeId(cari) {
        return cari?.cariTipId
            || state.cariTypes.find(item => item.adi === cari?.cariTipAdi)?.id
            || '';
    }

    function getCariTypeById(typeId) {
        return state.cariTypes.find(item => item.id === typeId) || null;
    }

    function ensureActiveCariType() {
        if (state.activeCariTypeId && getCariTypeById(state.activeCariTypeId)) {
            return state.activeCariTypeId;
        }

        state.activeCariTypeId = state.cariTypes[0]?.id || '';
        return state.activeCariTypeId;
    }

    function syncCariModalType(typeId = ensureActiveCariType()) {
        const input = byId('cariTipId');
        const info = byId('selectedCariTypeInfo');

        if (input) {
            input.value = typeId || '';
        }

        if (!info) {
            return;
        }

        const selectedType = getCariTypeById(typeId);
        if (!selectedType) {
            info.innerHTML = '<strong>Cari tipi secilmedi.</strong><small>Yeni kayit acmadan once sayfadaki cari tipi kartlarindan birini secin.</small>';
            return;
        }

        info.innerHTML = `<strong>${apiClient.escapeHtml(selectedType.adi)} secili</strong><small>CariTipId: ${apiClient.escapeHtml(typeId)}. Tipi degistirmek icin sayfadaki cari tipi kartlarini kullanin.</small>`;
    }

    function renderCariTypeSelector() {
        const container = byId('cariTypeChips');
        const summary = byId('cariTypeSelectionSummary');

        if (!container) {
            return;
        }

        if (state.cariTypes.length === 0) {
            container.innerHTML = '<div class="empty-state" style="padding: 8px 0;">Bu sirket icin tanimli cari tipi bulunmuyor.</div>';
            if (summary) {
                summary.innerHTML = '<strong>Cari tipi bulunamadi</strong><small>Yeni cari kart acmak icin once bir cari tipi tanimli olmali.</small>';
            }
            syncCariModalType('');
            return;
        }

        const activeTypeId = ensureActiveCariType();
        const activeType = getCariTypeById(activeTypeId);

        container.innerHTML = state.cariTypes.map(cariType => {
            const count = state.caris.filter(cari => resolveCariTypeId(cari) === cariType.id).length;
            const isActive = cariType.id === activeTypeId;

            return `
                <button
                    type="button"
                    class="cari-type-chip${isActive ? ' active' : ''}"
                    data-cari-type-id="${apiClient.escapeHtml(cariType.id)}"
                    aria-pressed="${isActive ? 'true' : 'false'}"
                >
                    <span class="cari-type-chip-title">${escapeText(cariType.adi)}</span>
                    <span class="cari-type-chip-meta">
                        <span>${count} kayit</span>
                        <span>${isActive ? 'Secili' : 'Sec'}</span>
                    </span>
                </button>
            `;
        }).join('');

        if (summary && activeType) {
            const activeCount = state.caris.filter(cari => resolveCariTypeId(cari) === activeTypeId).length;
            summary.innerHTML = `<strong>${apiClient.escapeHtml(activeType.adi)} secili</strong><small>${activeCount} kayit listeleniyor. Yeni cari kart bu tipe gore acilir.</small>`;
        }

        syncCariModalType(activeTypeId);
    }

    function setActiveCariType(typeId) {
        if (!getCariTypeById(typeId)) {
            return;
        }

        state.activeCariTypeId = typeId;
        renderCariTypeSelector();
        renderCaris();
    }

    function formatDate(value) {
        if (!value) {
            return '-';
        }

        const date = new Date(value);
        if (Number.isNaN(date.getTime())) {
            return '-';
        }

        return new Intl.DateTimeFormat('tr-TR', {
            dateStyle: 'medium',
            timeZone: 'Europe/Istanbul'
        }).format(date);
    }

    function setTableEmptyState(targetId, columnCount, message) {
        const tableBody = byId(targetId);
        if (!tableBody) {
            return;
        }

        tableBody.innerHTML = `<tr><td colspan="${columnCount}" class="empty-state">${apiClient.escapeHtml(message)}</td></tr>`;
    }

    function updateDashboardStats() {
        byId('stat-total-stocks').textContent = String(state.stocks.length);
        byId('stat-total-caris').textContent = String(state.caris.length);
        byId('stat-total-units').textContent = String(state.units.length);
        byId('stat-total-cari-types').textContent = String(state.cariTypes.length);
        byId('settings-unit-count').textContent = String(state.units.length);
        byId('settings-cari-type-count').textContent = String(state.cariTypes.length);
    }

    function renderReferenceSelects() {
        const filterUnit = byId('filterUnit');
        const unitSelect = byId('unitSelect');

        if (filterUnit) {
            const selectedValue = filterUnit.value;
            filterUnit.innerHTML = '<option value="">Tum Birimler</option>';
            state.units.forEach(unit => {
                filterUnit.insertAdjacentHTML(
                    'beforeend',
                    `<option value="${apiClient.escapeHtml(unit.id)}">${escapeText(unit.ad)} (${escapeText(unit.sembol)})</option>`
                );
            });
            filterUnit.value = state.units.some(unit => unit.id === selectedValue) ? selectedValue : '';
        }

        if (unitSelect) {
            const selectedValue = unitSelect.value;
            unitSelect.innerHTML = '<option value="">Birim Seciniz...</option>';
            state.units.forEach(unit => {
                unitSelect.insertAdjacentHTML(
                    'beforeend',
                    `<option value="${apiClient.escapeHtml(unit.id)}">${escapeText(unit.ad)} (${escapeText(unit.sembol)})</option>`
                );
            });
            unitSelect.value = state.units.some(unit => unit.id === selectedValue) ? selectedValue : '';
        }

        renderCariTypeSelector();
    }

    function renderSettings() {
        const unitsBody = byId('settingsUnitsBody');
        const cariTypesBody = byId('settingsCariTypesBody');

        if (unitsBody) {
            if (state.units.length === 0) {
                setTableEmptyState('settingsUnitsBody', 3, 'Bu sirket icin tanimli birim bulunmuyor.');
            } else {
                unitsBody.innerHTML = state.units.map(unit => `
                    <tr>
                        <td>${escapeText(unit.ad)}</td>
                        <td><code>${escapeText(unit.sembol)}</code></td>
                        <td><span class="badge badge-success">${unit.aktifMi === false ? 'Pasif' : 'Aktif'}</span></td>
                    </tr>
                `).join('');
            }
        }

        if (cariTypesBody) {
            if (state.cariTypes.length === 0) {
                setTableEmptyState('settingsCariTypesBody', 2, 'Bu sirket icin tanimli cari tip bulunmuyor.');
            } else {
                cariTypesBody.innerHTML = state.cariTypes.map(cariType => `
                    <tr>
                        <td>${escapeText(cariType.adi)}</td>
                        <td>${escapeText(cariType.aciklama, 'Musteri panelinde secilebilir tip.')}</td>
                    </tr>
                `).join('');
            }
        }
    }

    async function refreshReferenceData(forceRefresh = false) {
        if (!forceRefresh && state.units.length > 0 && state.cariTypes.length > 0) {
            renderReferenceSelects();
            renderSettings();
            updateDashboardStats();
            return;
        }

        const [units, cariTypes] = await Promise.all([
            apiClient.getUnits(),
            apiClient.getCariTypes()
        ]);

        state.units = Array.isArray(units) ? units : [];
        state.cariTypes = Array.isArray(cariTypes) ? cariTypes : [];

        renderReferenceSelects();
        renderSettings();
        updateDashboardStats();
    }

    function getFilteredStocks() {
        const searchTerm = (byId('stockSearch')?.value || '').trim().toLowerCase();
        const selectedUnitId = byId('filterUnit')?.value || '';

        return state.stocks.filter(stock => {
            const matchesSearch = !searchTerm
                || (stock.stokAdi || '').toLowerCase().includes(searchTerm)
                || (stock.stokKodu || '').toLowerCase().includes(searchTerm);
            const matchesUnit = !selectedUnitId || stock.birimId === selectedUnitId;
            return matchesSearch && matchesUnit;
        });
    }

    function renderStocks() {
        const stockTableBody = document.querySelector('#stockTable tbody');
        if (!stockTableBody) {
            return;
        }

        const filteredStocks = getFilteredStocks();
        if (filteredStocks.length === 0) {
            const message = state.stocks.length === 0
                ? 'Henuz stok karti bulunmuyor.'
                : 'Filtrelere uygun stok bulunamadi.';
            stockTableBody.innerHTML = `<tr><td colspan="5" class="empty-state">${apiClient.escapeHtml(message)}</td></tr>`;
            return;
        }

        stockTableBody.innerHTML = filteredStocks.map(stock => `
            <tr>
                <td style="font-weight: 600;">${escapeText(stock.stokKodu)}</td>
                <td>${escapeText(stock.stokAdi)}</td>
                <td>${escapeText(stock.birimAdi)}</td>
                <td>${escapeText(formatDate(stock.kayitTarihi))}</td>
                <td style="text-align: right;">
                    <div style="display: inline-flex; gap: 8px;">
                        <button type="button" class="action-btn edit-btn" onclick="openStockModal('${apiClient.escapeHtml(stock.id)}')" aria-label="Stok duzenle">
                            <i class="fas fa-pen"></i>
                        </button>
                        <button type="button" class="action-btn delete-btn" onclick="deleteStock('${apiClient.escapeHtml(stock.id)}')" aria-label="Stok sil">
                            <i class="fas fa-trash"></i>
                        </button>
                    </div>
                </td>
            </tr>
        `).join('');
    }

    async function loadStocks(forceRefresh = false) {
        if (!forceRefresh && state.stocks.length > 0) {
            renderStocks();
            updateDashboardStats();
            return;
        }

        const response = await apiClient.getStocks(1, 200);
        state.stocks = Array.isArray(response?.veriler) ? response.veriler : [];
        renderStocks();
        updateDashboardStats();
    }

    function getFilteredCaris() {
        const searchTerm = (byId('cariSearch')?.value || '').trim().toLowerCase();
        const activeCariTypeId = ensureActiveCariType();

        return state.caris.filter(cari => {
            const matchesType = !activeCariTypeId || resolveCariTypeId(cari) === activeCariTypeId;
            if (!matchesType) {
                return false;
            }

            if (!searchTerm) {
                return true;
            }

            return [
                cari.unvan,
                cari.adiSoyadi,
                cari.vtck_No,
                cari.vtckNo,
                cari.telefon,
                cari.gsm,
                cari.cariTipAdi
            ].some(value => (value || '').toLowerCase().includes(searchTerm));
        });
    }

    function renderCaris() {
        const cariList = byId('cariList');
        if (!cariList) {
            return;
        }

        const filteredCaris = getFilteredCaris();
        if (filteredCaris.length === 0) {
            const activeType = getCariTypeById(ensureActiveCariType());
            const message = state.caris.length === 0
                ? 'Henuz cari kart bulunmuyor.'
                : activeType
                    ? `${activeType.adi} tipinde cari kart bulunamadi.`
                    : 'Filtreye uygun cari kart bulunamadi.';
            cariList.innerHTML = `<tr><td colspan="6" class="empty-state">${apiClient.escapeHtml(message)}</td></tr>`;
            return;
        }

        cariList.innerHTML = filteredCaris.map(cari => {
            const displayName = cari.unvan || cari.adiSoyadi || '-';
            const vtck = cari.VTCK_No || cari.vtck_No || cari.vtckNo || '-';
            const contact = [cari.gsm, cari.telefon].filter(Boolean).join(' / ') || '-';
            const groupInfo = [cari.grupKodu, cari.ozelKodu].filter(Boolean).join(' / ') || '-';

            return `
                <tr>
                    <td><span class="badge badge-info">${escapeText(cari.cariTipAdi)}</span></td>
                    <td>${escapeText(displayName)}</td>
                    <td>${escapeText(vtck)}</td>
                    <td>${escapeText(contact)}</td>
                    <td>${escapeText(groupInfo)}</td>
                    <td style="text-align: right;">
                        <div style="display: inline-flex; gap: 8px;">
                            <button type="button" class="action-btn edit-btn" onclick="openCariModal('${apiClient.escapeHtml(cari.id)}')" aria-label="Cari duzenle">
                                <i class="fas fa-pen"></i>
                            </button>
                            <button type="button" class="action-btn delete-btn" onclick="deleteCari('${apiClient.escapeHtml(cari.id)}')" aria-label="Cari sil">
                                <i class="fas fa-trash"></i>
                            </button>
                        </div>
                    </td>
                </tr>
            `;
        }).join('');
    }

    async function loadCaris(forceRefresh = false) {
        if (!forceRefresh && state.caris.length > 0) {
            renderCaris();
            updateDashboardStats();
            return;
        }

        const response = await apiClient.getCariCards(1, 200);
        state.caris = Array.isArray(response?.veriler) ? response.veriler : [];
        renderCariTypeSelector();
        renderCaris();
        updateDashboardStats();
    }

    function openModal(modalId) {
        const modal = byId(modalId);
        if (modal) {
            modal.style.display = 'flex';
        }
    }

    function closeModal(modalId) {
        const modal = byId(modalId);
        if (modal) {
            modal.style.display = 'none';
        }
    }

    async function openStockModal(stockId) {
        await refreshReferenceData();

        const form = byId('stockForm');
        const modalTitle = byId('stockModalTitle');
        const editStockId = byId('editStockId');

        if (!form || !modalTitle || !editStockId) {
            return;
        }

        form.reset();
        editStockId.value = '';

        if (stockId) {
            const stock = state.stocks.find(item => item.id === stockId);
            if (!stock) {
                alert('Stok kaydi bulunamadi.');
                return;
            }

            modalTitle.textContent = 'Stok Karti Duzenle';
            editStockId.value = stock.id;
            byId('stokAdi').value = stock.stokAdi || '';
            byId('unitSelect').value = stock.birimId || '';
        } else {
            modalTitle.textContent = 'Yeni Stok Karti Olustur';
        }

        openModal('stockModal');
    }

    function closeStockModal() {
        closeModal('stockModal');
    }

    async function submitStockForm(event) {
        event.preventDefault();

        const stockId = byId('editStockId')?.value || '';
        const payload = {
            stokAdi: byId('stokAdi')?.value.trim(),
            yedekAdi: '',
            birimId: byId('unitSelect')?.value
        };

        if (!payload.stokAdi || !payload.birimId) {
            alert('Stok adi ve birim secimi zorunludur.');
            return;
        }

        try {
            if (stockId) {
                await apiClient.updateStock(stockId, payload);
            } else {
                await apiClient.createStock(payload);
            }

            closeStockModal();
            await loadStocks(true);
        } catch (error) {
            alert(error.message || 'Stok kaydi sirasinda bir hata olustu.');
        }
    }

    async function deleteStock(stockId) {
        if (!confirm('Bu stok kartini silmek istiyor musunuz?')) {
            return;
        }

        try {
            await apiClient.deleteStock(stockId);
            await loadStocks(true);
        } catch (error) {
            alert(error.message || 'Stok silinemedi.');
        }
    }

    async function openCariModal(cariId) {
        await refreshReferenceData();

        const form = byId('cariForm');
        const modalTitle = byId('cariModalTitle');
        const editCariId = byId('editCariId');

        if (!form || !modalTitle || !editCariId) {
            return;
        }

        form.reset();
        editCariId.value = '';
        byId('faturaTipi').value = '1';

        const defaultCariTypeId = ensureActiveCariType();
        if (!defaultCariTypeId) {
            alert('Yeni cari kart acmak icin once bir cari tipi bulunmali.');
            return;
        }

        if (cariId) {
            const cari = state.caris.find(item => item.id === cariId);
            if (!cari) {
                alert('Cari kaydi bulunamadi.');
                return;
            }
            const selectedCariTypeId = resolveCariTypeId(cari);

            modalTitle.textContent = 'Cari Kart Duzenle';
            editCariId.value = cari.id;
            state.activeCariTypeId = selectedCariTypeId || defaultCariTypeId;
            byId('faturaTipi').value = String(cari.faturaTipi || 1);
            byId('unvan').value = cari.unvan || '';
            byId('adiSoyadi').value = cari.adiSoyadi || '';
            byId('vtckNo').value = cari.VTCK_No || cari.vtck_No || cari.vtckNo || '';
            byId('vergiDairesi').value = cari.vergiDairesi || '';
            byId('gsm').value = cari.gsm || '';
            byId('telefon').value = cari.telefon || '';
            byId('grupKodu').value = cari.grupKodu || '';
            byId('ozelKodu').value = cari.ozelKodu || '';
            byId('adres').value = cari.adres || '';
        } else {
            modalTitle.textContent = 'Yeni Cari Kart';
            state.activeCariTypeId = defaultCariTypeId;
        }

        renderCariTypeSelector();
        openModal('cariModal');
    }

    function closeCariModal() {
        closeModal('cariModal');
    }

    async function submitCariForm(event) {
        event.preventDefault();

        const cariId = byId('editCariId')?.value || '';
        const payload = {
            cariTipId: byId('cariTipId')?.value,
            faturaTipi: Number.parseInt(byId('faturaTipi')?.value || '1', 10),
            unvan: byId('unvan')?.value.trim(),
            adiSoyadi: byId('adiSoyadi')?.value.trim(),
            VTCK_No: byId('vtckNo')?.value.trim(),
            vergiDairesi: byId('vergiDairesi')?.value.trim(),
            gsm: byId('gsm')?.value.trim(),
            telefon: byId('telefon')?.value.trim(),
            telefon2: '',
            grupKodu: byId('grupKodu')?.value.trim(),
            ozelKodu: byId('ozelKodu')?.value.trim(),
            adres: byId('adres')?.value.trim()
        };

        if (!payload.cariTipId) {
            alert('Cari tipi secimi zorunludur.');
            return;
        }

        try {
            if (cariId) {
                await apiClient.updateCariCard(cariId, payload);
            } else {
                await apiClient.createCariCard(payload);
            }

            closeCariModal();
            await loadCaris(true);
        } catch (error) {
            alert(error.message || 'Cari kaydi sirasinda bir hata olustu.');
        }
    }

    async function deleteCari(cariId) {
        if (!confirm('Bu cari karti silmek istiyor musunuz?')) {
            return;
        }

        try {
            await apiClient.deleteCariCard(cariId);
            await loadCaris(true);
        } catch (error) {
            alert(error.message || 'Cari kart silinemedi.');
        }
    }

    async function loadSettings(forceRefresh = false) {
        try {
            await refreshReferenceData(forceRefresh);
        } catch (error) {
            setTableEmptyState('settingsUnitsBody', 3, 'Birimler yuklenemedi.');
            setTableEmptyState('settingsCariTypesBody', 2, 'Cari tipleri yuklenemedi.');
            alert(error.message || 'Ayarlar yuklenemedi.');
        }
    }

    function handleHashChange() {
        showSection(readHashSection(), false);
    }

    async function bootstrap() {
        const stockForm = byId('stockForm');
        const cariForm = byId('cariForm');
        const stockSearch = byId('stockSearch');
        const filterUnit = byId('filterUnit');
        const cariSearch = byId('cariSearch');
        const cariTypeChips = byId('cariTypeChips');

        stockForm?.addEventListener('submit', submitStockForm);
        cariForm?.addEventListener('submit', submitCariForm);
        stockSearch?.addEventListener('input', renderStocks);
        filterUnit?.addEventListener('change', renderStocks);
        cariSearch?.addEventListener('input', renderCaris);
        cariTypeChips?.addEventListener('click', event => {
            const button = event.target.closest('[data-cari-type-id]');
            if (!button) {
                return;
            }

            setActiveCariType(button.getAttribute('data-cari-type-id'));
        });

        document.querySelectorAll('.modal-overlay').forEach(modal => {
            modal.addEventListener('click', event => {
                if (event.target === modal) {
                    modal.style.display = 'none';
                }
            });
        });

        window.addEventListener('hashchange', handleHashChange);
        showSection(readHashSection(), false);

        try {
            await Promise.all([
                refreshReferenceData(true),
                loadStocks(true),
                loadCaris(true)
            ]);
        } catch (error) {
            console.error('Customer panel bootstrap failed.', error);
        }
    }

    window.showSection = showSection;
    window.openStockModal = openStockModal;
    window.closeStockModal = closeStockModal;
    window.deleteStock = deleteStock;
    window.openCariModal = openCariModal;
    window.closeCariModal = closeCariModal;
    window.deleteCari = deleteCari;
    window.loadSettings = loadSettings;

    document.addEventListener('DOMContentLoaded', bootstrap);
})();
