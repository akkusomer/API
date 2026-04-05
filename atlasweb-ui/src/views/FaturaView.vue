<template>
  <AppShell
    :active-section="'fatura'"
    :current-route="route.path"
    :navigation="navigation"
    :top-menus="topMenus"
    :display-name="displayName"
    :company-label="companyLabel"
    :status-text="statusText"
    :show-topbar="false"
    @logout="logout"
  >
    <section class="invoice-workspace">
      <p v-if="pageError" class="form-error">{{ pageError }}</p>
      <p v-if="formError" class="form-error">{{ formError }}</p>
      <p v-if="formSuccess" class="form-success">{{ formSuccess }}</p>

      <template v-if="screenMode === 'list'">
        <header class="invoice-workspace__header">
          <div class="invoice-workspace__copy">
            <h1>Fatura Listesi</h1>
          </div>

          <div class="invoice-workspace__actions">
            <button type="button" class="tool-button tool-button--accent" @click="startNewInvoice">
              <span>Yeni Fatura</span>
              <small class="tool-button__shortcut">F2</small>
            </button>
            <button
              type="button"
              class="tool-button"
              :class="{ 'tool-button--neutral': showColumnManager }"
              @click="showColumnManager = !showColumnManager"
            >
              <span>Kolonlar</span>
            </button>
            <button type="button" class="tool-button tool-button--neutral" @click="loadInvoices">
              <span>Yenile</span>
              <small class="tool-button__shortcut">Alt+R</small>
            </button>
            <button
              type="button"
              class="tool-button tool-button--danger"
              :disabled="!selectedInvoiceId"
              @click="removeSelectedInvoice"
            >
              <span>Sil</span>
              <small class="tool-button__shortcut">F3</small>
            </button>
            <button
              type="button"
              class="tool-button"
              :disabled="!selectedInvoice?.cariKartId"
              @click="openCariEkstre()"
            >
              <span>Cari Ekstre</span>
              <small class="tool-button__shortcut">F10</small>
            </button>
          </div>
        </header>

        <WindowPanel title="Faturalar" class="invoice-list-panel">
          <div class="window-toolbar">
            <label class="inline-field inline-field--grow">
              <span>Ara</span>
              <input v-model.trim="search" type="text" placeholder="Fatura no, cari veya açıklama" />
            </label>
          </div>

          <div v-if="showColumnManager" class="column-manager">
            <div class="column-manager__header">
              <strong>Kolon Sirasi</strong>
              <button type="button" class="tool-button tool-button--neutral" @click="resetInvoiceColumns">
                Varsayilana Don
              </button>
            </div>

            <div class="column-manager__list">
              <div
                v-for="(column, index) in visibleInvoiceColumns"
                :key="`manager-${column.key}`"
                class="column-manager__item"
              >
                <span class="column-manager__label">{{ index + 1 }}. {{ column.label }}</span>
                <div class="column-manager__actions">
                  <button
                    type="button"
                    class="table-action"
                    :disabled="index === 0"
                    @click="moveInvoiceColumn(column.key, 'left')"
                  >
                    Sola
                  </button>
                  <button
                    type="button"
                    class="table-action"
                    :disabled="index === visibleInvoiceColumns.length - 1"
                    @click="moveInvoiceColumn(column.key, 'right')"
                  >
                    Saga
                  </button>
                </div>
              </div>
            </div>
          </div>

          <div class="grid-shell">
            <table class="data-grid">
              <thead>
                <tr>
                  <th
                    v-for="column in visibleInvoiceColumns"
                    :key="column.key"
                    draggable="true"
                    class="invoice-column-header"
                    :class="{
                      'is-dragging': draggedColumnKey === column.key,
                      'is-drop-target': dropTargetKey === column.key,
                      'align-right': column.align === 'right',
                    }"
                    @dragstart="handleColumnDragStart(column.key)"
                    @dragenter.prevent="handleColumnDragEnter(column.key)"
                    @dragover.prevent="handleColumnDragOver(column.key)"
                    @drop.prevent="handleColumnDrop(column.key)"
                    @dragend="handleColumnDragEnd"
                  >
                    <div class="invoice-column-header__content">
                      <span>{{ column.label }}</span>
                      <div class="invoice-column-header__actions">
                        <button
                          type="button"
                          class="invoice-column-header__move"
                          :disabled="visibleInvoiceColumns[0]?.key === column.key"
                          @click.stop="moveInvoiceColumn(column.key, 'left')"
                        >
                          ←
                        </button>
                        <button
                          type="button"
                          class="invoice-column-header__move"
                          :disabled="visibleInvoiceColumns[visibleInvoiceColumns.length - 1]?.key === column.key"
                          @click.stop="moveInvoiceColumn(column.key, 'right')"
                        >
                          →
                        </button>
                      </div>
                    </div>
                  </th>
                </tr>
              </thead>
              <tbody>
                <tr v-if="loading">
                  <td :colspan="visibleInvoiceColumns.length" class="empty-cell">Fatura kayıtları yükleniyor...</td>
                </tr>
                <tr v-else-if="filteredInvoices.length === 0">
                  <td :colspan="visibleInvoiceColumns.length" class="empty-cell">Fatura bulunamadı.</td>
                </tr>
                <tr
                  v-for="invoice in filteredInvoices"
                  :key="invoice.id"
                  class="is-clickable"
                  :class="{ 'is-selected': selectedInvoiceId === invoice.id }"
                  @click="selectInvoice(invoice)"
                  @dblclick="openInvoiceEditor(invoice)"
                >
                  <td
                    v-for="column in visibleInvoiceColumns"
                    :key="column.key"
                    :class="{ 'align-right': column.align === 'right' }"
                  >
                    {{ formatInvoiceCell(invoice, column.key) }}
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        </WindowPanel>
      </template>

      <template v-else>
        <header class="invoice-workspace__header">
          <div class="invoice-workspace__copy">
            <h1>{{ editingId ? 'Fatura Düzenle' : 'Yeni Fatura' }}</h1>
            <p>Sidebar sonrası kalan alan doğrudan fatura kesme ekranıdır.</p>
          </div>

          <div class="invoice-workspace__actions">
            <button type="button" class="tool-button tool-button--neutral" @click="returnToList">
              <span>Listeye Dön</span>
            </button>
            <button
              type="button"
              class="tool-button"
              :class="{ 'tool-button--neutral': !editingId }"
              :disabled="saving || loading || satisKunyeLoading || satisKunyeSubmitting"
              @click="openSatisKunyeModal"
            >
              <span>{{ satisKunyeActionLabel }}</span>
            </button>
            <button
              type="button"
              class="tool-button tool-button--danger"
              :disabled="!editingId || saving"
              @click="removeCurrentInvoice"
            >
              <span>Faturayı Sil</span>
              <small class="tool-button__shortcut">F3</small>
            </button>
            <button type="button" class="tool-button tool-button--accent" :disabled="saving || loading" @click="openTahsilatPrompt">
              <span>{{ saving ? 'Kaydediliyor...' : 'Kaydet' }}</span>
              <small class="tool-button__shortcut">F9</small>
            </button>
          </div>
        </header>

        <WindowPanel title="Fatura Formu" class="invoice-editor-panel">
          <div class="invoice-editor-panel__body">
            <div class="invoice-editor-meta">
              <label class="field">
                <span>Fatura No</span>
                <input :value="currentInvoiceNo || 'Kaydedildiğinde oluşacak'" type="text" readonly />
              </label>

              <label class="field">
                <span>Cari</span>
                <div
                  ref="cariPickerRef"
                  class="invoice-cari-picker"
                  @focusout="handleCariPickerFocusOut"
                >
                  <input
                    ref="cariInputRef"
                    v-model.trim="cariSearch"
                    type="text"
                    class="invoice-cari-picker__input"
                    placeholder="Cari ara, Enter ile tüm kayıtları getir"
                    autocomplete="off"
                    required
                    :disabled="loading || saving"
                    @input="handleCariSearchInput"
                    @focus="handleCariFocus"
                    @keydown="handleCariSearchKeydown"
                  />

                  <div v-if="cariDropdownOpen" class="invoice-cari-picker__dropdown">
                    <button
                      v-for="(cari, index) in visibleCariOptions"
                      :key="cari.id"
                      type="button"
                      class="invoice-cari-picker__option"
                      :class="{ 'is-active': index === cariActiveIndex, 'is-selected': cari.id === form.cariKartId }"
                      @mousedown.prevent="selectCariOption(cari)"
                      @click.prevent="selectCariOption(cari)"
                      @mouseenter="cariActiveIndex = index"
                    >
                      <strong>{{ formatCariOptionLabel(cari) }}</strong>
                      <span>{{ formatCariOptionMeta(cari) }}</span>
                    </button>

                    <div v-if="visibleCariOptions.length === 0" class="invoice-cari-picker__empty">
                      Eşleşen cari bulunamadı.
                    </div>
                  </div>
                </div>
              </label>

              <label class="field">
                <span>Fatura Tarihi</span>
                <input v-model="form.faturaTarihi" type="date" required :disabled="loading || saving" />
              </label>

            </div>

            <div class="window-panel window-panel--muted invoice-editor-lines">
              <header class="window-panel__header">
                <div class="window-panel__titlebar">
                  <span class="window-panel__dot" />
                  <h2>Fatura Kalemleri</h2>
                </div>

                <div class="window-panel__actions">
                  <span class="invoice-editor-shortcuts">F2 yeni, F3 sil, F9 kaydet, Ctrl+Enter künye getir</span>
                  <button type="button" class="tool-button tool-button--neutral" @click="resetInvoiceLineColumnOrder">
                    Kalem Kolonlarını Sıfırla
                  </button>
                  <button type="button" class="tool-button" @click="addLine">Satır Ekle</button>
                </div>
              </header>

              <div class="window-panel__body">
                <div class="grid-shell">
                  <table class="data-grid invoice-lines-grid">
                    <thead>
                      <tr>
                        <th
                          v-for="column in visibleInvoiceLineColumns"
                          :key="`line-header-${column.key}`"
                          draggable="true"
                          class="invoice-column-header"
                          :class="{
                            'is-dragging': draggedInvoiceLineColumnKey === column.key,
                            'is-drop-target': invoiceLineDropTargetKey === column.key,
                          }"
                          @dragstart="handleInvoiceLineDragStart(column.key)"
                          @dragenter.prevent="handleInvoiceLineDragEnter(column.key)"
                          @dragover.prevent="handleInvoiceLineDragOver(column.key)"
                          @drop.prevent="handleInvoiceLineDrop(column.key)"
                          @dragend="handleInvoiceLineDragEnd"
                        >
                          <span>{{ column.label }}</span>
                        </th>
                        <th>Satır Toplamı</th>
                        <th class="align-right">İşlem</th>
                      </tr>
                    </thead>
                    <tbody>
                      <tr v-for="(line, index) in form.kalemler" :key="index">
                        <td
                          v-for="column in visibleInvoiceLineColumns"
                          :key="`line-${index}-${column.key}`"
                        >
                          <div v-if="column.key === 'stokId'">
                            <div
                              :ref="(element) => setStockPickerRef(index, element)"
                              class="invoice-cari-picker invoice-stock-picker"
                              @focusout="handleStockPickerFocusOut(index)"
                            >
                              <input
                                v-model.trim="line.stokArama"
                                type="text"
                                class="invoice-cari-picker__input"
                                placeholder="Stok ara, Enter ile tüm kayıtları getir"
                                autocomplete="off"
                                required
                                :disabled="loading || saving"
                                :ref="(element) => setLineCellRef(index, 'stokId', element)"
                                @input="handleStockSearchInput(index)"
                                @focus="handleStockFocus(index)"
                                @keydown="handleStockSearchKeydown($event, index)"
                              />

                              <div v-if="stockDropdownRow === index" class="invoice-cari-picker__dropdown">
                                <button
                                  v-for="(stock, stockIndex) in visibleStockOptions"
                                  :key="stock.id"
                                  type="button"
                                  class="invoice-cari-picker__option"
                                  :class="{ 'is-active': stockIndex === stockActiveIndex, 'is-selected': stock.id === line.stokId }"
                                  @mousedown.prevent="selectStockOption(index, stock)"
                                  @click.prevent="selectStockOption(index, stock)"
                                  @mouseenter="stockActiveIndex = stockIndex"
                                >
                                  <strong>{{ formatStockOptionLabel(stock) }}</strong>
                                  <span>{{ formatStockOptionMeta(stock) }}</span>
                                </button>

                                <div v-if="visibleStockOptions.length === 0" class="invoice-cari-picker__empty">
                                  Eşleşen stok bulunamadı.
                                </div>
                              </div>
                            </div>
                          </div>

                          <input
                            v-else-if="column.key === 'alisKunye'"
                            v-model.trim="line.alisKunye"
                            type="text"
                            maxlength="120"
                            placeholder="Alış künyesi"
                            :ref="(element) => setLineCellRef(index, 'alisKunye', element)"
                            @keydown="handleLineCellKeydown($event, index, 'alisKunye')"
                          />

                          <input
                            v-else-if="column.key === 'satisKunye'"
                            :value="line.satisKunye || ''"
                            type="text"
                            maxlength="120"
                            placeholder="Satis kunyesi"
                            readonly
                            :ref="(element) => setLineCellRef(index, 'satisKunye', element)"
                            @keydown="handleLineCellKeydown($event, index, 'satisKunye')"
                          />

                          <input
                            v-else-if="column.key === 'miktar'"
                            v-model.number="line.miktar"
                            type="number"
                            min="0.001"
                            step="0.001"
                            required
                            :ref="(element) => setLineCellRef(index, 'miktar', element)"
                            @keydown="handleLineCellKeydown($event, index, 'miktar')"
                          />

                          <input
                            v-else-if="column.key === 'birimFiyat'"
                            v-model.number="line.birimFiyat"
                            type="number"
                            min="0"
                            step="0.01"
                            required
                            :ref="(element) => setLineCellRef(index, 'birimFiyat', element)"
                            @keydown="handleLineCellKeydown($event, index, 'birimFiyat')"
                          />
                        </td>
                        <td>{{ formatMoney(lineTotal(line)) }}</td>
                        <td class="align-right">
                          <button
                            type="button"
                            class="table-action table-action--danger"
                            @click="removeLine(index)"
                            :disabled="form.kalemler.length === 1"
                          >
                            Sil
                          </button>
                        </td>
                      </tr>
                    </tbody>
                  </table>
                </div>

                <div class="invoice-total-strip">
                  <small>Genel Toplam</small>
                  <strong>{{ formatMoney(grandTotal) }}</strong>
                </div>
              </div>
            </div>

            <div class="invoice-editor-footer">
              <label class="field field--full invoice-editor-notes">
                <span>Açıklama</span>
                <textarea
                  v-model.trim="form.aciklama"
                  rows="4"
                  placeholder="Fatura notu veya açıklama"
                  :disabled="saving"
                />
              </label>

              <div class="invoice-editor-summary">
                <small>Aktif Kayıt</small>
                <strong>{{ currentInvoiceNo || 'Yeni Fatura' }}</strong>
                <span>{{ form.kalemler.length }} kalem hazır</span>
              </div>
            </div>
          </div>
        </WindowPanel>
      </template>
    </section>

    <div v-if="satisKunyeModalOpen" class="modal-backdrop" @click.self="closeSatisKunyeModal">
      <div class="modal-window modal-window--small satis-kunye-modal">
        <header class="modal-window__header">
          <div>
            <h2>Satis Kunye Bildirimi</h2>
            <p class="modal-window__copy">
              Kayitli fatura kalemleri icin HKS satis kunyesi olustur.
            </p>
          </div>

          <button type="button" class="ghost-icon" @click="closeSatisKunyeModal">×</button>
        </header>

        <div class="desktop-form tahsilat-modal__body">
          <p v-if="satisKunyeError" class="form-error">{{ satisKunyeError }}</p>
          <p v-else-if="satisKunyeInfo" class="form-success">{{ satisKunyeInfo }}</p>

          <div v-if="satisKunyeLoading" class="progress-card">
            <div class="progress-card__head">
              <strong>HKS bildirim bilgileri hazirlaniyor</strong>
            </div>
            <div class="progress-card__track">
              <div class="progress-card__fill" style="width: 100%"></div>
            </div>
          </div>

          <template v-else>
            <div class="tahsilat-modal__banner">
              <strong>{{ currentInvoiceNo || 'Kayitli Fatura' }}</strong>
            </div>

            <div class="satis-kunye-modal__summary">
              <div class="satis-kunye-modal__summary-row">
                <span>Islenecek Kalem</span>
                <strong>{{ missingSatisKunyeLineCount }}</strong>
              </div>
              <div class="satis-kunye-modal__summary-row">
                <span>Genel Toplam</span>
                <strong>{{ formatMoney(grandTotal) }}</strong>
              </div>
            </div>

            <div class="satis-kunye-modal__grid">
              <label class="field">
                <span>Bildirimci Sifat</span>
                <select v-model="satisKunyeForm.bildirimciSifatId">
                  <option value="">Sifat seciniz</option>
                  <option
                    v-for="option in satisKunyeSifatOptions"
                    :key="`satis-sifat-${option.id}`"
                    :value="String(option.id)"
                  >
                    {{ option.ad }}
                  </option>
                </select>
              </label>

              <label class="field">
                <span>Bildirim Turu</span>
                <select v-model="satisKunyeForm.bildirimTuruId">
                  <option value="">Bildirim turu seciniz</option>
                  <option
                    v-for="option in satisKunyeBildirimTurleri"
                    :key="`bildirim-turu-${option.id}`"
                    :value="String(option.id)"
                  >
                    {{ option.ad }}
                  </option>
                </select>
              </label>

              <label class="field">
                <span>Belge Tipi</span>
                <select v-model="satisKunyeForm.belgeTipiId">
                  <option value="">Belge tipi seciniz</option>
                  <option
                    v-for="option in satisKunyeBelgeTipleri"
                    :key="`belge-tipi-${option.id}`"
                    :value="String(option.id)"
                  >
                    {{ option.ad }}
                  </option>
                </select>
              </label>

              <label class="field">
                <span>Belge No</span>
                <input v-model.trim="satisKunyeForm.belgeNo" type="text" maxlength="50" />
              </label>
            </div>

            <div class="grid-shell satis-kunye-modal__lines">
              <table class="data-grid">
                <thead>
                  <tr>
                    <th>Stok</th>
                    <th>Alis Kunye</th>
                    <th>Miktar</th>
                    <th>Satis Kunye</th>
                  </tr>
                </thead>
                <tbody>
                  <tr v-if="satisKunyeLinePreview.length === 0">
                    <td colspan="4" class="empty-cell">Islenecek kalem bulunmuyor.</td>
                  </tr>
                  <tr v-for="line in satisKunyeLinePreview" :key="line.key">
                    <td>{{ line.stokLabel }}</td>
                    <td>{{ line.alisKunye || '-' }}</td>
                    <td>{{ line.miktarLabel }}</td>
                    <td>{{ line.satisKunye || '-' }}</td>
                  </tr>
                </tbody>
              </table>
            </div>
          </template>
        </div>

        <footer class="modal-window__footer">
          <button type="button" class="tool-button tool-button--neutral" @click="closeSatisKunyeModal">
            <span>Vazgec</span>
          </button>
          <button
            type="button"
            class="tool-button tool-button--accent"
            :disabled="satisKunyeLoading || satisKunyeSubmitting || missingSatisKunyeLineCount === 0"
            @click="submitSatisKunye"
          >
            <span>{{ satisKunyeSubmitting ? 'Gonderiliyor...' : 'Satis Kunyeleri Al' }}</span>
          </button>
        </footer>
      </div>
    </div>

    <div v-if="referansKunyeModalOpen" class="modal-backdrop" @click.self="closeReferansKunyeModal">
      <div class="modal-window modal-window--wide referans-kunye-modal">
        <header class="modal-window__header">
          <div>
            <h2>Referans Künyeler</h2>
            <p class="modal-window__copy">
              {{ referansKunyeStockLabel || 'Seçili stok' }} için HKS referans künyeleri
            </p>
          </div>

          <button type="button" class="ghost-icon" @click="closeReferansKunyeModal">×</button>
        </header>

        <div class="window-panel__body referans-kunye-modal__body">
          <p v-if="referansKunyeError" class="form-error">{{ referansKunyeError }}</p>
          <p v-else-if="referansKunyeInfo" class="form-success">{{ referansKunyeInfo }}</p>

          <div v-if="referansKunyeLoading" class="progress-card">
            <div class="progress-card__head">
              <strong>HKS referans künye sorgusu yapılıyor</strong>
            </div>
            <div class="progress-card__track">
              <div class="progress-card__fill" style="width: 100%"></div>
            </div>
            <p class="muted-line">Seçili stok için ürün bazlı sonuçlar getiriliyor.</p>
          </div>

          <div v-else class="grid-shell">
            <table class="data-grid">
              <thead>
                <tr>
                  <th>Künye No</th>
                  <th>Bildirim Tarihi</th>
                  <th>Mal</th>
                  <th>Kalan Miktar</th>
                  <th>Mal Sahibi</th>
                  <th class="align-right">İşlem</th>
                </tr>
              </thead>
              <tbody>
                <tr v-if="referansKunyeOptions.length === 0">
                  <td colspan="6" class="empty-cell">Bu stok için referans künye bulunamadı.</td>
                </tr>
                <tr
                  v-for="item in referansKunyeOptions"
                  :key="item.uniqueId || item.kunyeNo"
                  class="is-clickable"
                  @dblclick="applyReferansKunye(item)"
                >
                  <td>{{ item.kunyeNo }}</td>
                  <td>{{ formatDate(item.bildirimTarihi) }}</td>
                  <td>
                    <strong>{{ item.malinAdi || '-' }}</strong>
                    <div class="muted-line">Kod: {{ item.malinKodNo || '-' }}</div>
                  </td>
                  <td>{{ formatReferansKunyeQuantity(item) }}</td>
                  <td>{{ item.malinSahibiTcKimlikVergiNo || '-' }}</td>
                  <td class="align-right">
                    <button type="button" class="table-action" @click="applyReferansKunye(item)">
                      Seç
                    </button>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>

        <footer class="modal-window__footer">
          <button type="button" class="tool-button tool-button--neutral" @click="closeReferansKunyeModal">
            <span>Kapat</span>
          </button>
        </footer>
      </div>
    </div>

    <div v-if="tahsilatPromptOpen" class="modal-backdrop" @click.self="closeTahsilatPrompt">
      <div class="modal-window modal-window--small tahsilat-modal">
        <header class="modal-window__header">
          <div>
            <h2>Tahsilat</h2>
          </div>

          <button type="button" class="ghost-icon" @click="closeTahsilatPrompt">×</button>
        </header>

        <div class="desktop-form tahsilat-modal__body">
          <div class="tahsilat-modal__banner">
            <strong>{{ tahsilatPromptTitle }}</strong>
          </div>

          <div class="tahsilat-modal__summary">
            <div class="tahsilat-modal__summary-row">
              <span>Belge Toplamı</span>
              <strong>{{ formatMoney(grandTotal) }}</strong>
            </div>

            <label class="field tahsilat-modal__field">
              <span>Belge Tahsilatı</span>
              <input
                ref="tahsilatInputRef"
                v-model="tahsilatAmount"
                type="number"
                min="0"
                :max="grandTotal"
                step="0.01"
                placeholder="Tahsil edilen tutarı gir"
                @keydown.enter.prevent="confirmTahsilatAndSave"
              />
            </label>
          </div>
        </div>

        <footer class="modal-window__footer">
          <button type="button" class="tool-button tool-button--neutral" @click="closeTahsilatPrompt">
            <span>Vazgeç</span>
            <small class="tool-button__shortcut">Esc</small>
          </button>
          <button type="button" class="tool-button tool-button--accent" @click="confirmTahsilatAndSave">
            <span>Kaydet ve Çık</span>
            <small class="tool-button__shortcut">F9</small>
          </button>
        </footer>
      </div>
    </div>
  </AppShell>
</template>

<script setup>
import { computed, nextTick, onBeforeUnmount, onMounted, reactive, ref } from 'vue'
import AppShell from '../components/AppShell.vue'
import WindowPanel from '../components/WindowPanel.vue'
import { useColumnOrder } from '../composables/useColumnOrder'
import { useGridKeyboard } from '../composables/useGridKeyboard'
import { useSaveShortcut } from '../composables/useSaveShortcut'
import { useWorkspaceShell } from '../composables/useWorkspaceShell'
import { apiClient } from '../services/api'

const DEFAULT_INVOICE_COLUMNS = [
  { key: 'faturaNo', label: 'Fatura No' },
  { key: 'cariAdi', label: 'Cari' },
  { key: 'faturaTarihi', label: 'Tarih' },
  { key: 'alisKunye', label: 'Alis Kunya' },
  { key: 'kalemSayisi', label: 'Kalem', align: 'right' },
  { key: 'tutar', label: 'Tutar', align: 'right' },
  { key: 'aciklama', label: 'Aciklama' },
]

const DEFAULT_INVOICE_LINE_COLUMNS = [
  { key: 'stokId', label: 'Stok' },
  { key: 'alisKunye', label: 'Alis Kunya' },
  { key: 'satisKunye', label: 'Satis Kunya' },
  { key: 'miktar', label: 'Miktar' },
  { key: 'birimFiyat', label: 'Birim Fiyat' },
]

const screenMode = ref('list')
const loading = ref(true)
const saving = ref(false)
const search = ref('')
const invoices = ref([])
const caris = ref([])
const cariOptions = ref([])
const stocks = ref([])
const showColumnManager = ref(false)
const cariSearch = ref('')
const cariDropdownOpen = ref(false)
const cariShowAll = ref(false)
const cariActiveIndex = ref(-1)
const cariPickerRef = ref(null)
const cariInputRef = ref(null)
const stockOptions = ref([])
const stockDropdownRow = ref(-1)
const stockShowAll = ref(false)
const stockActiveIndex = ref(-1)
const editingId = ref('')
const currentInvoiceNo = ref('')
const formError = ref('')
const formSuccess = ref('')
const pageError = ref('')
const selectedInvoiceId = ref('')
const tahsilatPromptOpen = ref(false)
const tahsilatAmount = ref('')
const tahsilatInputRef = ref(null)
const satisKunyeModalOpen = ref(false)
const satisKunyeLoading = ref(false)
const satisKunyeSubmitting = ref(false)
const satisKunyeError = ref('')
const satisKunyeInfo = ref('')
const satisKunyeSifatOptions = ref([])
const satisKunyeBildirimTurleri = ref([])
const satisKunyeBelgeTipleri = ref([])
const referansKunyeModalOpen = ref(false)
const referansKunyeLoading = ref(false)
const referansKunyeError = ref('')
const referansKunyeInfo = ref('')
const referansKunyeOptions = ref([])
const referansKunyeTargetRow = ref(-1)
const referansKunyeStockLabel = ref('')
const lineCellRefs = new Map()
const stockPickerRefs = new Map()
let cariSearchRequestId = 0
let stockSearchRequestId = 0

const form = reactive({
  cariKartId: '',
  faturaTarihi: '',
  aciklama: '',
  tahsilEdilenTutar: null,
  kalemler: [createEmptyLine()],
})

const satisKunyeForm = reactive({
  bildirimciSifatId: '',
  bildirimTuruId: '',
  belgeTipiId: '',
  belgeNo: '',
})

const statusText = computed(() => {
  if (saving.value) {
    return 'Fatura kaydediliyor'
  }

  if (screenMode.value === 'editor') {
    return editingId.value ? `${currentInvoiceNo.value || 'Kayıtlı fatura'} açık` : 'Yeni fatura hazırlanıyor'
  }

  return `${filteredInvoices.value.length} fatura listeleniyor`
})

const {
  route,
  router,
  displayName,
  companyLabel,
  navigation,
  topMenus,
  logout,
} = useWorkspaceShell('fatura', statusText)

const {
  visibleColumns: visibleInvoiceColumns,
  moveColumn: moveInvoiceColumn,
  resetColumns: resetInvoiceColumnOrder,
  draggedColumnKey,
  dropTargetKey,
  handleColumnDragStart,
  handleColumnDragEnter,
  handleColumnDragOver,
  handleColumnDrop,
  handleColumnDragEnd,
} = useColumnOrder('invoice-list', DEFAULT_INVOICE_COLUMNS)

const {
  visibleColumns: visibleInvoiceLineColumns,
  resetColumns: resetInvoiceLineColumnOrder,
  draggedColumnKey: draggedInvoiceLineColumnKey,
  dropTargetKey: invoiceLineDropTargetKey,
  handleColumnDragStart: handleInvoiceLineDragStart,
  handleColumnDragEnter: handleInvoiceLineDragEnter,
  handleColumnDragOver: handleInvoiceLineDragOver,
  handleColumnDrop: handleInvoiceLineDrop,
  handleColumnDragEnd: handleInvoiceLineDragEnd,
} = useColumnOrder('invoice-editor-lines', DEFAULT_INVOICE_LINE_COLUMNS)

function resetInvoiceColumns() {
  resetInvoiceColumnOrder()
}

const lineFieldOrder = computed(() => visibleInvoiceLineColumns.value.map((column) => column.key))

const filteredInvoices = computed(() => {
  const term = search.value.trim().toLowerCase()
  if (!term) {
    return invoices.value
  }

  return invoices.value.filter((invoice) =>
    [invoice.faturaNo, invoice.cariAdi, invoice.alisKunye, invoice.aciklama]
      .filter(Boolean)
      .some((value) => value.toLowerCase().includes(term))
  )
})

const visibleCariOptions = computed(() => cariOptions.value)
const visibleStockOptions = computed(() => stockOptions.value)

const grandTotal = computed(() =>
  form.kalemler.reduce((sum, line) => sum + lineTotal(line), 0)
)

const missingSatisKunyeLineCount = computed(() =>
  form.kalemler.filter((line) => line.stokId && !line.satisKunye).length
)

const satisKunyeActionLabel = computed(() => {
  if (saving.value) {
    return 'Kaydediliyor...'
  }

  if (satisKunyeLoading.value) {
    return 'Hazirlaniyor...'
  }

  if (satisKunyeSubmitting.value) {
    return 'Gonderiliyor...'
  }

  return 'Satis Kunya'
})

const selectedInvoice = computed(() =>
  filteredInvoices.value.find((invoice) => invoice.id === selectedInvoiceId.value) || null
)

const satisKunyeLinePreview = computed(() =>
  form.kalemler
    .filter((line) => line.stokId)
    .map((line, index) => ({
      key: line.id || `${index}-${line.stokId}`,
      stokLabel: line.stokArama || formatStockOptionLabel(resolveSelectedStock(line) || {}),
      alisKunye: line.alisKunye || '',
      satisKunye: line.satisKunye || '',
      miktarLabel: new Intl.NumberFormat('tr-TR', {
        minimumFractionDigits: 0,
        maximumFractionDigits: 3,
      }).format(Number(line.miktar || 0)),
    }))
)

const tahsilatPromptTitle = computed(() => {
  if (currentInvoiceNo.value) {
    return `${currentInvoiceNo.value} Nolu Satis Faturasi`
  }

  return 'Yeni Hal Satis Faturasi'
})

const { selectItem: selectInvoice } = useGridKeyboard({
  items: filteredInvoices,
  selectedKey: selectedInvoiceId,
  setSelectedKey: (value) => {
    selectedInvoiceId.value = value
  },
  enabled: computed(() => screenMode.value === 'list'),
  onCreate: () => startNewInvoice(),
  onEnter: (invoice) => openInvoiceEditor(invoice),
  onDelete: (invoice) => removeInvoice(invoice.id),
  onF10: (invoice) => openCariEkstre(invoice.cariKartId),
})

useSaveShortcut({
  enabled: computed(() => screenMode.value === 'editor' && !saving.value && !loading.value),
  onSave: handleSaveShortcut,
})

function createEmptyLine() {
  return {
    id: '',
    stokId: '',
    stokArama: '',
    alisKunye: '',
    satisKunye: '',
    miktar: 1,
    birimFiyat: 0,
  }
}

function resetSatisKunyeModalState() {
  satisKunyeLoading.value = false
  satisKunyeSubmitting.value = false
  satisKunyeError.value = ''
  satisKunyeInfo.value = ''
  satisKunyeSifatOptions.value = []
  satisKunyeBildirimTurleri.value = []
  satisKunyeBelgeTipleri.value = []
  satisKunyeForm.bildirimciSifatId = ''
  satisKunyeForm.bildirimTuruId = ''
  satisKunyeForm.belgeTipiId = ''
  satisKunyeForm.belgeNo = currentInvoiceNo.value || ''
}

function closeSatisKunyeModal() {
  satisKunyeModalOpen.value = false
  resetSatisKunyeModalState()
}

function preferHksOption(options, keywords = []) {
  if (!Array.isArray(options) || options.length === 0) {
    return null
  }

  const normalizedKeywords = keywords
    .map((keyword) => normalizeSearchText(keyword))
    .filter(Boolean)

  if (normalizedKeywords.length === 0) {
    return options[0]
  }

  return options.find((option) => {
    const label = normalizeSearchText(option?.ad || '')
    return normalizedKeywords.some((keyword) => label.includes(keyword))
  }) || options[0]
}

function formatSatisKunyeError(error, fallbackMessage) {
  const parts = []
  const message = error?.message || error?.hata || error?.mesaj || error?.Mesaj || error?.title || fallbackMessage

  if (message) {
    parts.push(message)
  }

  const islemKodu = error?.islemKodu || error?.IslemKodu
  if (islemKodu) {
    parts.push(`Islem kodu: ${islemKodu}`)
  }

  const hataKodlari = error?.hataKodlari || error?.HataKodlari
  if (Array.isArray(hataKodlari) && hataKodlari.length > 0) {
    const formattedCodes = hataKodlari
      .map((item) => {
        if (!item) {
          return ''
        }

        if (typeof item === 'string') {
          return item
        }

        const code = item.hataKodu ?? item.HataKodu ?? item.code ?? item.Code
        const text = item.mesaj ?? item.Mesaj ?? item.message ?? item.Message
        if (code && text) {
          return `${code}: ${text}`
        }

        return text || code || ''
      })
      .filter(Boolean)

    if (formattedCodes.length > 0) {
      parts.push(`Hata kodlari: ${formattedCodes.join(', ')}`)
    }
  }

  const validationErrors = error?.errors
  if (validationErrors && typeof validationErrors === 'object') {
    const validationMessage = Object.entries(validationErrors)
      .flatMap(([field, value]) => {
        const items = Array.isArray(value) ? value : [value]
        return items
          .filter(Boolean)
          .map((item) => `${field}: ${String(item)}`)
      })
      .join(' | ')

    if (validationMessage) {
      parts.push(validationMessage)
    }
  }

  return parts.join(' | ')
}

function getPersistableInvoiceLines() {
  return form.kalemler.filter((line) => line.stokId)
}

function mergeUniqueCariOptions(items = []) {
  const map = new Map()

  items.forEach((item) => {
    if (item?.id && !map.has(item.id)) {
      map.set(item.id, item)
    }
  })

  return Array.from(map.values())
}

function pickCariMatch(term, candidates = []) {
  const normalizedTerm = normalizeSearchText(term)
  if (!normalizedTerm || !Array.isArray(candidates) || candidates.length === 0) {
    return null
  }

  const exactMatch = candidates.find((cari) =>
    [
      cari.unvan,
      cari.adiSoyadi,
      cari.VTCK_No || cari.vtckNo,
      formatCariOptionLabel(cari),
    ]
      .filter(Boolean)
      .some((value) => normalizeSearchText(value) === normalizedTerm)
  )

  if (exactMatch) {
    return exactMatch
  }

  return candidates.length === 1 ? candidates[0] : null
}

async function ensureCariSelectionForSave() {
  if (form.cariKartId) {
    return true
  }

  const term = cariSearch.value.trim()
  if (!term) {
    return false
  }

  const localCandidates = mergeUniqueCariOptions([
    ...cariOptions.value,
    ...caris.value.filter((cari) => matchesCariSearch(cari, term)),
  ])

  let matchedCari = pickCariMatch(term, localCandidates)

  if (!matchedCari) {
    await searchCariOptions(term, false)

    matchedCari = pickCariMatch(term, mergeUniqueCariOptions([
      ...cariOptions.value,
      ...caris.value.filter((cari) => matchesCariSearch(cari, term)),
    ]))
  }

  if (!matchedCari) {
    return false
  }

  await selectCariOption(matchedCari)
  return true
}

function resolveInvoiceLineLabel(line, index) {
  return line?.stokArama || formatStockOptionLabel(resolveSelectedStock(line) || {}) || `${index + 1}. kalem`
}

function validateInvoiceBeforeSave(cariSelectionResolved = true) {
  if (!form.cariKartId) {
    return cariSearch.value.trim() && !cariSelectionResolved
      ? 'Cariyi listeden secmelisiniz. Enter ile kaydi secin veya tam eslesen tek cari kullanin.'
      : 'Once cari secmelisiniz.'
  }

  if (!form.faturaTarihi) {
    return 'Fatura tarihi zorunludur.'
  }

  const lines = getPersistableInvoiceLines()
  if (lines.length === 0) {
    return 'En az bir fatura kalemi girmelisiniz.'
  }

  const invalidQuantityLine = lines.find((line) => !Number.isFinite(Number(line.miktar)) || Number(line.miktar) <= 0)
  if (invalidQuantityLine) {
    return `${resolveInvoiceLineLabel(invalidQuantityLine, lines.indexOf(invalidQuantityLine))} icin miktar 0'dan buyuk olmalidir.`
  }

  const invalidPriceLine = lines.find((line) => !Number.isFinite(Number(line.birimFiyat)) || Number(line.birimFiyat) < 0)
  if (invalidPriceLine) {
    return `${resolveInvoiceLineLabel(invalidPriceLine, lines.indexOf(invalidPriceLine))} icin birim fiyat gecersiz.`
  }

  return ''
}

function formatPersistError(error, fallbackMessage) {
  const parts = [error?.message || error?.hata || error?.title || fallbackMessage]

  const detail = error?.detay || error?.Detail || error?.detail
  if (detail && detail !== error?.message) {
    parts.push(detail)
  }

  if (error?.errors && typeof error.errors === 'object') {
    const validationMessage = Object.entries(error.errors)
      .flatMap(([field, value]) => {
        const items = Array.isArray(value) ? value : [value]
        return items
          .filter(Boolean)
          .map((item) => `${field}: ${String(item)}`)
      })
      .join(' | ')

    if (validationMessage) {
      parts.push(validationMessage)
    }
  }

  return parts.filter(Boolean).join(' | ')
}

function buildInvoicePayload() {
  const kalemler = getPersistableInvoiceLines().map((line) => ({
    stokId: line.stokId,
    alisKunye: line.alisKunye || null,
    satisKunye: line.satisKunye || null,
    miktar: Number(line.miktar),
    birimFiyat: Number(line.birimFiyat),
  }))

  return {
    cariKartId: form.cariKartId,
    faturaTarihi: `${form.faturaTarihi}T00:00:00Z`,
    aciklama: form.aciklama || null,
    tahsilEdilenTutar: Number(form.tahsilEdilenTutar || 0),
    kalemler,
  }
}

function applyInvoiceDetail(detail) {
  editingId.value = detail.id || editingId.value
  currentInvoiceNo.value = detail.faturaNo || currentInvoiceNo.value || ''
  form.cariKartId = detail.cariKartId || ''
  syncCariSearchFromSelection()
  form.faturaTarihi = toInputDate(detail.faturaTarihi)
  form.aciklama = detail.aciklama || ''
  form.tahsilEdilenTutar = Number(detail.tahsilEdilenTutar ?? 0)
  form.kalemler = (detail.kalemler || []).length
    ? detail.kalemler.map((item) => ({
      id: item.id || '',
      stokId: item.stokId,
      stokArama: '',
      alisKunye: item.alisKunye || '',
      satisKunye: item.satisKunye || '',
      miktar: Number(item.miktar) || 0,
      birimFiyat: Number(item.birimFiyat) || 0,
    }))
    : [createEmptyLine()]
  syncAllStockSearches()
}

async function openSatisKunyeModal() {
  if (saving.value || satisKunyeLoading.value || satisKunyeSubmitting.value) {
    return
  }

  resetMessages()

  if (!editingId.value) {
    const saved = await persistInvoice({ returnToList: false, showSuccess: false, refreshAfterSave: true })
    if (!saved) {
      satisKunyeModalOpen.value = false
      satisKunyeError.value = formError.value || 'Satis kunye icin fatura kaydedilemedi.'
      return
    }
  }

  resetSatisKunyeModalState()
  satisKunyeModalOpen.value = true
  satisKunyeLoading.value = true

  try {
    const [settings, bildirimTurleri, belgeTipleri] = await Promise.all([
      apiClient.getHksSettings(),
      apiClient.getHksBildirimTurleri(),
      apiClient.getHksBelgeTipleri(),
    ])

    if (!settings?.kullaniciAdi) {
      throw new Error('HKS kullanici adi ayarlarda tanimli degil.')
    }

    const kayitliKisi = await apiClient.getHksKayitliKisiSorgu(settings.kullaniciAdi)
    const sellerSifatlar = Array.isArray(kayitliKisi?.sifatlar) ? kayitliKisi.sifatlar : []
    if (!kayitliKisi?.kayitliKisiMi || sellerSifatlar.length === 0) {
      throw new Error('HKS kullanicisi icin bildirimci sifat bulunamadi.')
    }

    satisKunyeSifatOptions.value = sellerSifatlar
    satisKunyeBildirimTurleri.value = Array.isArray(bildirimTurleri) ? bildirimTurleri : []
    satisKunyeBelgeTipleri.value = Array.isArray(belgeTipleri) ? belgeTipleri : []

    satisKunyeForm.bildirimciSifatId = String(preferHksOption(sellerSifatlar)?.id || '')
    satisKunyeForm.bildirimTuruId = String(preferHksOption(satisKunyeBildirimTurleri.value, ['satis', 'cikis'])?.id || '')
    satisKunyeForm.belgeTipiId = String(preferHksOption(satisKunyeBelgeTipleri.value, ['fatura', 'satis'])?.id || '')
    satisKunyeForm.belgeNo = currentInvoiceNo.value || ''

    satisKunyeInfo.value = missingSatisKunyeLineCount.value > 0
      ? `${missingSatisKunyeLineCount.value} kalem icin satis kunyesi alinacak.`
      : 'Tum kalemlerde satis kunyesi mevcut.'
  } catch (error) {
    const message = formatSatisKunyeError(error, 'Satis kunye bilgileri hazirlanamadi.')
    satisKunyeError.value = message
    formError.value = message
  } finally {
    satisKunyeLoading.value = false
  }
}

async function submitSatisKunye() {
  if (!editingId.value) {
    satisKunyeError.value = 'Kayitli fatura bulunamadi.'
    return
  }

  if (!satisKunyeForm.bildirimciSifatId || !satisKunyeForm.bildirimTuruId || !satisKunyeForm.belgeTipiId) {
    satisKunyeError.value = 'Tum HKS bildirim alanlarini secmelisiniz.'
    return
  }

  satisKunyeSubmitting.value = true
  satisKunyeError.value = ''
  satisKunyeInfo.value = ''
  formError.value = ''

  try {
    const response = await apiClient.generateInvoiceSalesKunye(editingId.value, {
      bildirimciSifatId: Number(satisKunyeForm.bildirimciSifatId),
      bildirimTuruId: Number(satisKunyeForm.bildirimTuruId),
      belgeTipiId: Number(satisKunyeForm.belgeTipiId),
      belgeNo: satisKunyeForm.belgeNo || currentInvoiceNo.value || null,
    })

    const responseLines = Array.isArray(response?.kalemler) ? response.kalemler : []
    const lineMap = new Map(responseLines.map((line) => [line.detayId, line]))
    form.kalemler = form.kalemler.map((line) => {
      const updated = line.id ? lineMap.get(line.id) : null
      if (!updated) {
        return line
      }

      return {
        ...line,
        satisKunye: updated.satisKunye || line.satisKunye || '',
      }
    })

    formSuccess.value = response?.mesaj || 'Satis kunyeleri olusturuldu.'
    closeSatisKunyeModal()
  } catch (error) {
    const message = formatSatisKunyeError(error, 'Satis kunyeleri alinamadi.')
    satisKunyeError.value = message
    formError.value = message
  } finally {
    satisKunyeSubmitting.value = false
  }
}

function createDefaultReferansKunyeDates() {
  const today = new Date()
  const start = new Date(today)
  start.setDate(today.getDate() - 29)

  return {
    baslangicTarihi: `${toInputDate(start)}T00:00:00`,
    bitisTarihi: `${toInputDate(today)}T23:59:59`,
  }
}

function resolveSelectedStock(line) {
  if (!line?.stokId) {
    return null
  }

  return stocks.value.find((item) => item.id === line.stokId)
    || stockOptions.value.find((item) => item.id === line.stokId)
    || null
}

function closeReferansKunyeModal() {
  referansKunyeModalOpen.value = false
  referansKunyeLoading.value = false
  referansKunyeError.value = ''
  referansKunyeInfo.value = ''
  referansKunyeOptions.value = []
  referansKunyeTargetRow.value = -1
  referansKunyeStockLabel.value = ''
}

function formatReferansKunyeQuantity(item) {
  const value = item?.kalanMiktar ?? item?.malinMiktari
  if (value === null || value === undefined) {
    return '-'
  }

  const formatted = new Intl.NumberFormat('tr-TR', {
    minimumFractionDigits: 0,
    maximumFractionDigits: 3,
  }).format(Number(value))

  const unit = item?.miktarBirimiAd || item?.miktarBirimId
  return unit ? `${formatted} ${unit}` : formatted
}

function resolveReferansKunyeAvailableQuantity(item) {
  const value = Number(item?.kalanMiktar ?? item?.malinMiktari ?? 0)
  return Number.isFinite(value) ? value : 0
}

async function fetchReferansKunyelerForLine(rowIndex) {
  const line = form.kalemler[rowIndex]
  const stock = resolveSelectedStock(line)
  const requestedQuantity = Math.max(Number(line?.miktar) || 0, 0)

  if (!line?.stokId || !stock) {
    formError.value = 'Önce stok seçmelisiniz.'
    return
  }

  if (!stock.hksUrunId) {
    formError.value = 'Seçili stok için HKS ürün tanımı bulunmuyor.'
    return
  }

  resetMessages()
  referansKunyeModalOpen.value = true
  referansKunyeLoading.value = true
  referansKunyeError.value = ''
  referansKunyeInfo.value = ''
  referansKunyeOptions.value = []
  referansKunyeTargetRow.value = rowIndex
  referansKunyeStockLabel.value = formatStockOptionLabel(stock)

  try {
    const savedSnapshot = await apiClient.getSavedHksReferansKunyeler().catch(() => null)
    const defaults = createDefaultReferansKunyeDates()
    const payload = {
      urunId: Number(stock.hksUrunId),
      kalanMiktariSifirdanBuyukOlanlar: true,
      baslangicTarihi: savedSnapshot?.baslangicTarihi || defaults.baslangicTarihi,
      bitisTarihi: savedSnapshot?.bitisTarihi || defaults.bitisTarihi,
    }

    const response = await apiClient.searchHksReferansKunyelerInstant(payload)
    const productMatches = (Array.isArray(response?.referansKunyeler) ? response.referansKunyeler : [])
      .filter((item) => !item?.malinKodNo || Number(item.malinKodNo) === Number(stock.hksUrunId))
    const filtered = (requestedQuantity > 0
      ? productMatches.filter((item) => resolveReferansKunyeAvailableQuantity(item) >= requestedQuantity)
      : productMatches
    ).sort((left, right) => {
        if (requestedQuantity > 0) {
          const leftDelta = resolveReferansKunyeAvailableQuantity(left) - requestedQuantity
          const rightDelta = resolveReferansKunyeAvailableQuantity(right) - requestedQuantity
          if (leftDelta !== rightDelta) {
            return leftDelta - rightDelta
          }
        }

        const leftDate = left?.bildirimTarihi ? new Date(left.bildirimTarihi).getTime() : 0
        const rightDate = right?.bildirimTarihi ? new Date(right.bildirimTarihi).getTime() : 0
        if (rightDate !== leftDate) {
          return rightDate - leftDate
        }

        return Number(right?.kunyeNo || 0) - Number(left?.kunyeNo || 0)
      })

    referansKunyeOptions.value = filtered

    if (filtered.length === 1) {
      applyReferansKunye(filtered[0])
      return
    }

    if (filtered.length > 0) {
      referansKunyeInfo.value = requestedQuantity > 0
        ? `${requestedQuantity} miktarını karşılayan ${filtered.length} referans künye bulundu.`
        : `${filtered.length} referans künye bulundu. Satıra yazmak için seçim yapın.`
      return
    }

    referansKunyeInfo.value = requestedQuantity > 0 && productMatches.length > 0
      ? `Girilen ${requestedQuantity} miktarını karşılayan referans künye bulunamadı.`
      : 'Bu stok için referans künye bulunamadı.'
  } catch (error) {
    referansKunyeError.value = error.message || 'HKS referans künyeleri alınamadı.'
  } finally {
    referansKunyeLoading.value = false
  }
}

function applyReferansKunye(item) {
  const targetRow = referansKunyeTargetRow.value
  if (targetRow < 0 || !form.kalemler[targetRow]) {
    closeReferansKunyeModal()
    return
  }

  form.kalemler[targetRow].alisKunye = item?.kunyeNo ? String(item.kunyeNo) : ''
  formSuccess.value = `${form.kalemler[targetRow].stokArama || 'Stok'} için alış künyesi eklendi.`
  closeReferansKunyeModal()
  focusLineCell(targetRow, 'alisKunye')
}

function formatInvoiceCell(invoice, columnKey) {
  switch (columnKey) {
    case 'faturaNo':
      return invoice.faturaNo || '-'
    case 'cariAdi':
      return invoice.cariAdi || '-'
    case 'faturaTarihi':
      return formatDate(invoice.faturaTarihi)
    case 'alisKunye':
      return invoice.alisKunye || '-'
    case 'kalemSayisi':
      return invoice.kalemSayisi ?? 0
    case 'tutar':
      return formatMoney(invoice.tutar)
    case 'aciklama':
      return invoice.aciklama || '-'
    default:
      return '-'
  }
}

function resetMessages() {
  formError.value = ''
  formSuccess.value = ''
  pageError.value = ''
}

function resetForm() {
  editingId.value = ''
  currentInvoiceNo.value = ''
  form.cariKartId = ''
  cariSearch.value = ''
  cariDropdownOpen.value = false
  cariShowAll.value = false
  cariActiveIndex.value = -1
  stockOptions.value = []
  stockDropdownRow.value = -1
  stockShowAll.value = false
  stockActiveIndex.value = -1
  form.faturaTarihi = toInputDate()
  form.aciklama = ''
  form.tahsilEdilenTutar = null
  form.kalemler = [createEmptyLine()]
  tahsilatPromptOpen.value = false
  tahsilatAmount.value = ''
  satisKunyeModalOpen.value = false
  resetSatisKunyeModalState()
  closeReferansKunyeModal()
}

function resolveTahsilatAmount() {
  const existingAmount = Number(form.tahsilEdilenTutar)
  if (Number.isFinite(existingAmount) && existingAmount >= 0) {
    return existingAmount.toFixed(2)
  }

  return '0.00'
}

async function openTahsilatPrompt() {
  resetMessages()
  tahsilatAmount.value = resolveTahsilatAmount()
  tahsilatPromptOpen.value = true
  await nextTick()
  tahsilatInputRef.value?.focus()
  tahsilatInputRef.value?.select?.()
}

function closeTahsilatPrompt() {
  tahsilatPromptOpen.value = false
}

async function handleSaveShortcut() {
  if (tahsilatPromptOpen.value) {
    await confirmTahsilatAndSave()
    return
  }

  await openTahsilatPrompt()
}

function formatCariOptionLabel(cari) {
  return cari.unvan || cari.adiSoyadi || 'Adsız Cari'
}

function formatCariOptionMeta(cari) {
  return [cari.VTCK_No || cari.vtckNo, cari.gsm || cari.telefon]
    .filter(Boolean)
    .join(' • ')
}

function formatStockOptionLabel(stock) {
  return [stock.stokKodu, stock.stokAdi].filter(Boolean).join(' - ') || 'Adsız Stok'
}

function formatStockOptionMeta(stock) {
  return [stock.yedekAdi, [stock.birimSembolu, stock.birimAdi].filter(Boolean).join(' / ')]
    .filter(Boolean)
    .join(' • ')
}

function normalizeSearchText(value) {
  return String(value || '')
    .toLocaleLowerCase('tr-TR')
    .replaceAll('ç', 'c')
    .replaceAll('ğ', 'g')
    .replaceAll('ı', 'i')
    .replaceAll('ö', 'o')
    .replaceAll('ş', 's')
    .replaceAll('ü', 'u')
    .trim()
}

function upsertRecord(list, record) {
  if (!record?.id) {
    return
  }

  const existingIndex = list.findIndex((item) => item.id === record.id)
  if (existingIndex === -1) {
    list.unshift(record)
    return
  }

  list.splice(existingIndex, 1, { ...list[existingIndex], ...record })
}

function matchesCariSearch(cari, term) {
  const normalizedTerm = normalizeSearchText(term)

  if (!normalizedTerm) {
    return true
  }

  return [
    cari.unvan,
    cari.adiSoyadi,
    cari.VTCK_No || cari.vtckNo,
    cari.gsm,
    cari.telefon,
    formatCariOptionLabel(cari),
    formatCariOptionMeta(cari),
  ]
    .filter(Boolean)
    .some((value) => normalizeSearchText(value).includes(normalizedTerm))
}

function matchesStockSearch(stock, term) {
  const normalizedTerm = normalizeSearchText(term)

  if (!normalizedTerm) {
    return true
  }

  return [
    stock.stokKodu,
    stock.stokAdi,
    stock.yedekAdi,
    stock.birimAdi,
    stock.birimSembolu,
    formatStockOptionLabel(stock),
    formatStockOptionMeta(stock),
  ]
    .filter(Boolean)
    .some((value) => normalizeSearchText(value).includes(normalizedTerm))
}

function syncCariSearchFromSelection() {
  const selectedCari = cariOptions.value.find((item) => item.id === form.cariKartId)
    || caris.value.find((item) => item.id === form.cariKartId)
  cariSearch.value = selectedCari ? formatCariOptionLabel(selectedCari) : ''
}

function syncStockSearchForLine(line) {
  const selectedStock = stockOptions.value.find((item) => item.id === line.stokId)
    || stocks.value.find((item) => item.id === line.stokId)
  line.stokArama = selectedStock ? formatStockOptionLabel(selectedStock) : ''
}

function syncAllStockSearches() {
  form.kalemler.forEach((line) => syncStockSearchForLine(line))
}

function openCariDropdown(showAll = false) {
  cariShowAll.value = showAll
  cariDropdownOpen.value = true
  cariActiveIndex.value = visibleCariOptions.value.length ? 0 : -1
}

async function searchCariOptions(term = '', showAll = false) {
  const normalized = term.trim()
  cariSearchRequestId += 1
  const requestId = cariSearchRequestId

  if (!normalized && !showAll) {
    cariOptions.value = []
    cariDropdownOpen.value = false
    cariShowAll.value = false
    cariActiveIndex.value = -1
    return
  }

  const response = await apiClient.getCaris({
    pageSize: showAll ? 1000 : 50,
    search: normalized,
  })

  if (requestId !== cariSearchRequestId) {
    return
  }

  const responseOptions = response?.veriler || []
  responseOptions.forEach((cari) => upsertRecord(caris.value, cari))
  cariOptions.value = normalized
    ? responseOptions.filter((cari) => matchesCariSearch(cari, normalized))
    : responseOptions
  cariShowAll.value = showAll
  cariDropdownOpen.value = true
  cariActiveIndex.value = cariOptions.value.length ? 0 : -1
}

async function handleCariFocus() {
  if (cariSearch.value.trim()) {
    await searchCariOptions(cariSearch.value, false)
  }
}

async function handleCariSearchInput() {
  form.cariKartId = ''
  await searchCariOptions(cariSearch.value, false)
}

function handleCariPickerFocusOut() {
  window.setTimeout(() => {
    if (!cariPickerRef.value?.contains(document.activeElement)) {
      cariDropdownOpen.value = false
      cariShowAll.value = false
      cariActiveIndex.value = -1
      cariOptions.value = []

      if (form.cariKartId) {
        syncCariSearchFromSelection()
      }
    }
  }, 0)
}

async function selectCariOption(cari) {
  upsertRecord(caris.value, cari)
  form.cariKartId = cari.id
  cariSearch.value = formatCariOptionLabel(cari)
  cariDropdownOpen.value = false
  cariShowAll.value = false
  cariActiveIndex.value = -1
  await nextTick()
  focusLineCell(0, 'stokId')
}

async function handleCariSearchKeydown(event) {
  if (event.key === 'ArrowDown') {
    event.preventDefault()

    if (!cariDropdownOpen.value) {
      await searchCariOptions(cariSearch.value, !cariSearch.value.trim())
      return
    }

    if (visibleCariOptions.value.length) {
      cariActiveIndex.value = Math.min(cariActiveIndex.value + 1, visibleCariOptions.value.length - 1)
    }
    return
  }

  if (event.key === 'ArrowUp') {
    event.preventDefault()

    if (!cariDropdownOpen.value) {
      await searchCariOptions(cariSearch.value, !cariSearch.value.trim())
      return
    }

    if (visibleCariOptions.value.length) {
      cariActiveIndex.value = Math.max(cariActiveIndex.value - 1, 0)
    }
    return
  }

  if (event.key === 'Enter') {
    event.preventDefault()

    if (!cariDropdownOpen.value) {
      await searchCariOptions(cariSearch.value, !cariSearch.value.trim())
      return
    }

    const activeCari = visibleCariOptions.value[cariActiveIndex.value]
    if (activeCari) {
      await selectCariOption(activeCari)
      return
    }

    await searchCariOptions('', true)
    return
  }

  if (event.key === 'Escape') {
    cariDropdownOpen.value = false
    cariShowAll.value = false
    cariActiveIndex.value = -1
    cariOptions.value = []
    if (form.cariKartId) {
      syncCariSearchFromSelection()
    }
  }
}

function setStockPickerRef(rowIndex, element) {
  if (element) {
    stockPickerRefs.set(rowIndex, element)
    return
  }

  stockPickerRefs.delete(rowIndex)
}

function closeStockDropdown() {
  stockOptions.value = []
  stockDropdownRow.value = -1
  stockShowAll.value = false
  stockActiveIndex.value = -1
}

async function searchStockOptions(rowIndex, term = '', showAll = false) {
  const normalized = term.trim()
  stockSearchRequestId += 1
  const requestId = stockSearchRequestId

  if (!normalized && !showAll) {
    closeStockDropdown()
    return
  }

  const response = await apiClient.getStocks(1, showAll ? 1000 : 50, normalized)

  if (requestId !== stockSearchRequestId) {
    return
  }

  const responseOptions = response?.veriler || []
  responseOptions.forEach((stock) => upsertRecord(stocks.value, stock))
  stockOptions.value = normalized
    ? responseOptions.filter((stock) => matchesStockSearch(stock, normalized))
    : responseOptions
  stockDropdownRow.value = rowIndex
  stockShowAll.value = showAll
  stockActiveIndex.value = stockOptions.value.length ? 0 : -1
}

async function handleStockFocus(rowIndex) {
  const line = form.kalemler[rowIndex]
  if (line?.stokArama?.trim()) {
    await searchStockOptions(rowIndex, line.stokArama, false)
  }
}

async function handleStockSearchInput(rowIndex) {
  const line = form.kalemler[rowIndex]
  if (!line) {
    return
  }

  line.stokId = ''
  await searchStockOptions(rowIndex, line.stokArama, false)
}

function handleStockPickerFocusOut(rowIndex) {
  window.setTimeout(() => {
    const picker = stockPickerRefs.get(rowIndex)
    if (!picker?.contains(document.activeElement) && stockDropdownRow.value === rowIndex) {
      closeStockDropdown()

      const line = form.kalemler[rowIndex]
      if (line?.stokId) {
        syncStockSearchForLine(line)
      }
    }
  }, 0)
}

function selectStockOption(rowIndex, stock) {
  const line = form.kalemler[rowIndex]
  if (!line) {
    return
  }

  upsertRecord(stocks.value, stock)
  line.stokId = stock.id
  line.stokArama = formatStockOptionLabel(stock)
  closeStockDropdown()
}

async function handleStockSearchKeydown(event, rowIndex) {
  const line = form.kalemler[rowIndex]
  if (!line) {
    return
  }

  if ((event.ctrlKey || event.metaKey) && event.key === 'Enter') {
    event.preventDefault()
    await fetchReferansKunyelerForLine(rowIndex)
    return
  }

  if (event.altKey || event.ctrlKey || event.metaKey || event.shiftKey) {
    return
  }

  if (event.key === 'ArrowDown') {
    event.preventDefault()

    if (stockDropdownRow.value !== rowIndex) {
      await searchStockOptions(rowIndex, line.stokArama, !line.stokArama.trim())
      return
    }

    if (visibleStockOptions.value.length) {
      stockActiveIndex.value = Math.min(stockActiveIndex.value + 1, visibleStockOptions.value.length - 1)
    }
    return
  }

  if (event.key === 'ArrowUp') {
    event.preventDefault()

    if (stockDropdownRow.value !== rowIndex) {
      await searchStockOptions(rowIndex, line.stokArama, !line.stokArama.trim())
      return
    }

    if (visibleStockOptions.value.length) {
      stockActiveIndex.value = Math.max(stockActiveIndex.value - 1, 0)
    }
    return
  }

  if (event.key === 'Enter') {
    event.preventDefault()

    if (stockDropdownRow.value !== rowIndex) {
      await searchStockOptions(rowIndex, line.stokArama, !line.stokArama.trim())
      return
    }

    const activeStock = visibleStockOptions.value[stockActiveIndex.value]
    if (activeStock) {
      selectStockOption(rowIndex, activeStock)
      await moveLineFocus(rowIndex, 'stokId', 1)
      return
    }

    await searchStockOptions(rowIndex, '', true)
    return
  }

  if (event.key === 'Escape') {
    closeStockDropdown()
    if (line.stokId) {
      syncStockSearchForLine(line)
    }
    return
  }

  if (event.key === 'ArrowRight' && stockDropdownRow.value !== rowIndex) {
    event.preventDefault()
    await moveLineFocus(rowIndex, 'stokId', 1)
    return
  }

  if (event.key === 'ArrowLeft' && stockDropdownRow.value !== rowIndex) {
    event.preventDefault()
    await moveLineFocus(rowIndex, 'stokId', -1)
  }
}

async function startNewInvoice() {
  resetMessages()
  resetForm()
  screenMode.value = 'editor'
  await nextTick()
  cariInputRef.value?.focus()
  cariInputRef.value?.select?.()
}

function returnToList() {
  screenMode.value = 'list'
  resetMessages()
}

function setLineCellRef(rowIndex, field, element) {
  const key = `${rowIndex}:${field}`
  if (element) {
    lineCellRefs.set(key, element)
    return
  }

  lineCellRefs.delete(key)
}

function focusLineCell(rowIndex, field) {
  nextTick(() => {
    const element = lineCellRefs.get(`${rowIndex}:${field}`)
    if (!element) {
      return
    }

    element.focus()
    if (typeof element.select === 'function' && element.tagName === 'INPUT') {
      element.select()
    }
  })
}

function resolveHorizontalTarget(rowIndex, field, step) {
  const currentOrder = lineFieldOrder.value
  const currentIndex = currentOrder.indexOf(field)
  if (currentIndex === -1) {
    return null
  }

  const nextIndex = currentIndex + step
  if (nextIndex >= 0 && nextIndex < currentOrder.length) {
    return { rowIndex, field: currentOrder[nextIndex] }
  }

  if (nextIndex < 0) {
    if (rowIndex === 0) {
      return null
    }

    return { rowIndex: rowIndex - 1, field: currentOrder[currentOrder.length - 1] }
  }

  return { rowIndex: rowIndex + 1, field: currentOrder[0] }
}

async function moveLineFocus(rowIndex, field, step) {
  const target = resolveHorizontalTarget(rowIndex, field, step)
  if (!target) {
    return
  }

  if (target.rowIndex >= form.kalemler.length) {
    form.kalemler.push(createEmptyLine())
    await nextTick()
  }

  focusLineCell(target.rowIndex, target.field)
}

async function handleLineCellKeydown(event, rowIndex, field) {
  if ((event.ctrlKey || event.metaKey) && event.key === 'Enter') {
    event.preventDefault()
    await fetchReferansKunyelerForLine(rowIndex)
    return
  }

  if (event.altKey || event.ctrlKey || event.metaKey || event.shiftKey) {
    return
  }

  if (event.key === 'ArrowRight' || event.key === 'Enter') {
    event.preventDefault()
    await moveLineFocus(rowIndex, field, 1)
    return
  }

  if (event.key === 'ArrowLeft') {
    event.preventDefault()
    await moveLineFocus(rowIndex, field, -1)
  }
}

function lineTotal(line) {
  const miktar = Number(line.miktar) || 0
  const birimFiyat = Number(line.birimFiyat) || 0
  return Math.round(miktar * birimFiyat * 100) / 100
}

function formatMoney(value) {
  return new Intl.NumberFormat('tr-TR', {
    style: 'currency',
    currency: 'TRY',
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  }).format(Number(value || 0))
}

function formatDate(value) {
  if (!value) {
    return '-'
  }

  return new Intl.DateTimeFormat('tr-TR', {
    dateStyle: 'short',
    timeZone: 'Europe/Istanbul',
  }).format(new Date(value))
}

function toInputDate(value) {
  const date = value ? new Date(value) : new Date()
  const year = date.getUTCFullYear()
  const month = `${date.getUTCMonth() + 1}`.padStart(2, '0')
  const day = `${date.getUTCDate()}`.padStart(2, '0')
  return `${year}-${month}-${day}`
}

async function loadReferenceData() {
  const [cariResponse, stockResponse] = await Promise.all([
    apiClient.getCaris({ pageSize: 1000 }),
    apiClient.getStocks(1, 1000),
  ])

  caris.value = cariResponse?.veriler || []
  stocks.value = stockResponse?.veriler || []
  syncCariSearchFromSelection()
  syncAllStockSearches()
}

async function loadInvoices() {
  loading.value = true
  pageError.value = ''

  try {
    const invoiceResponse = await apiClient.getInvoices()
    invoices.value = invoiceResponse?.veriler || []
  } catch (error) {
    pageError.value = error.message || 'Fatura kayıtları yüklenemedi.'
  } finally {
    loading.value = false
  }
}

async function loadPageData() {
  loading.value = true
  pageError.value = ''

  try {
    await Promise.all([
      loadReferenceData(),
      loadInvoices(),
    ])
  } catch (error) {
    pageError.value = error.message || 'Fatura verileri yüklenemedi.'
  } finally {
    loading.value = false
  }
}

async function openInvoiceEditor(invoice) {
  resetMessages()
  resetForm()
  screenMode.value = 'editor'
  editingId.value = invoice.id

  try {
    const detail = await apiClient.getInvoice(invoice.id)
    applyInvoiceDetail(detail)
  } catch (error) {
    formError.value = error.message || 'Fatura detayları yüklenemedi.'
  }
}

async function addLine() {
  form.kalemler.push(createEmptyLine())
  await nextTick()
  focusLineCell(form.kalemler.length - 1, 'stokId')
}

function removeLine(index) {
  if (form.kalemler.length === 1) {
    return
  }

  if (stockDropdownRow.value === index) {
    closeStockDropdown()
  }
  form.kalemler.splice(index, 1)
}

async function confirmTahsilatAndSave() {
  const parsedAmount = Number(tahsilatAmount.value)

  if (!Number.isFinite(parsedAmount) || parsedAmount < 0) {
    formError.value = 'Tahsil edilen tutar gecersiz.'
    return
  }

  if (parsedAmount > grandTotal.value) {
    formError.value = 'Tahsil edilen tutar fatura toplamindan buyuk olamaz.'
    return
  }

  form.tahsilEdilenTutar = Math.round(parsedAmount * 100) / 100
  closeTahsilatPrompt()
  await persistInvoice()
}

async function persistInvoice(options = {}) {
  const {
    returnToList = true,
    showSuccess = true,
    refreshAfterSave = false,
  } = options

  if (saving.value) {
    return false
  }

  resetMessages()

  const cariSelectionResolved = await ensureCariSelectionForSave()

  const validationMessage = validateInvoiceBeforeSave(cariSelectionResolved)
  if (validationMessage) {
    formError.value = validationMessage
    return false
  }

  saving.value = true

  try {
    const payload = buildInvoicePayload()

    if (editingId.value) {
      await apiClient.updateInvoice(editingId.value, payload)
      if (refreshAfterSave) {
        const detail = await apiClient.getInvoice(editingId.value)
        applyInvoiceDetail(detail)
      }

      if (showSuccess) {
        formSuccess.value = `${currentInvoiceNo.value || 'Fatura'} güncellendi.`
      }
    } else {
      const result = await apiClient.createInvoice(payload)
      editingId.value = result?.id || ''
      currentInvoiceNo.value = result?.faturaNo || ''

      if (refreshAfterSave && editingId.value) {
        const detail = await apiClient.getInvoice(editingId.value)
        applyInvoiceDetail(detail)
      }

      if (showSuccess) {
        formSuccess.value = `${currentInvoiceNo.value || 'Fatura'} oluşturuldu.`
      }
    }

    await loadInvoices()
    selectedInvoiceId.value = editingId.value
    if (returnToList) {
      screenMode.value = 'list'
    }
    return true
  } catch (error) {
    formError.value = formatPersistError(error, 'Fatura kaydedilemedi.')
    return false
  } finally {
    saving.value = false
  }
}

async function removeInvoice(id) {
  if (!window.confirm('Bu faturayı silmek istiyor musunuz?')) {
    return
  }

  resetMessages()

  try {
    await apiClient.deleteInvoice(id)
    await loadInvoices()
  } catch (error) {
    pageError.value = error.message || 'Fatura silinemedi.'
  }
}

async function removeSelectedInvoice() {
  if (!selectedInvoice.value) {
    return
  }

  await removeInvoice(selectedInvoice.value.id)
}

function openCariEkstre(cariKartId = '') {
  const targetCariId = cariKartId || form.cariKartId || selectedInvoice.value?.cariKartId
  if (!targetCariId) {
    return
  }

  router.push(`/cariler/${targetCariId}/ekstre`)
}

async function removeCurrentInvoice() {
  if (!editingId.value) {
    return
  }

  if (!window.confirm('Bu faturayı silmek istiyor musunuz?')) {
    return
  }

  resetMessages()

  try {
    await apiClient.deleteInvoice(editingId.value)
    formSuccess.value = `${currentInvoiceNo.value || 'Fatura'} pasife alindi.`
    await loadInvoices()
    returnToList()
    resetForm()
  } catch (error) {
    pageError.value = error.message || 'Fatura silinemedi.'
  }
}

async function handleShortcut(event) {
  const target = event.target
  const isF10 = event.key === 'F10'
  const isEscape = event.key === 'Escape'

  if (isEscape && tahsilatPromptOpen.value) {
    event.preventDefault()
    closeTahsilatPrompt()
    return
  }

  if (!isF10 && event.key !== 'F9' && target instanceof HTMLElement && target.closest('input, textarea, select, button, [contenteditable="true"]')) {
    return
  }

  if (event.defaultPrevented) {
    return
  }

  if (event.key === 'F2') {
    event.preventDefault()
    await startNewInvoice()
    return
  }

  if (screenMode.value === 'list' && event.altKey && event.key.toLowerCase() === 'r') {
    event.preventDefault()
    await loadInvoices()
    return
  }

  if (event.key === 'F3') {
    event.preventDefault()

    if (screenMode.value === 'editor') {
      await removeCurrentInvoice()
      return
    }

    const selected = filteredInvoices.value.find((item) => item.id === selectedInvoiceId.value)
    if (selected) {
      await removeInvoice(selected.id)
    }
    return
  }

  if (event.key === 'F10') {
    event.preventDefault()
    openCariEkstre()
    return
  }

  if (screenMode.value === 'list' && event.key === 'Enter') {
    const selected = filteredInvoices.value.find((item) => item.id === selectedInvoiceId.value)
    if (selected) {
      event.preventDefault()
      await openInvoiceEditor(selected)
    }
  }
}

onMounted(async () => {
  resetForm()
  await loadPageData()
  window.addEventListener('keydown', handleShortcut, true)
})

onBeforeUnmount(() => {
  window.removeEventListener('keydown', handleShortcut, true)
})
</script>
