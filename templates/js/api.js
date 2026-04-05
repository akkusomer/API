const JWT_CLAIMS = {
    role: 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role',
    email: 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress',
    name: 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name',
    userId: 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'
};

function resolveApiBaseUrl() {
    const explicitBaseUrl = window.ATLAS_API_BASE_URL?.trim();
    if (explicitBaseUrl) {
        return explicitBaseUrl.replace(/\/+$/, '');
    }

    if (window.location.protocol === 'http:' || window.location.protocol === 'https:') {
        return window.location.origin.replace(/\/+$/, '');
    }

    return 'http://34.159.204.209';
}

async function readResponseBody(response) {
    if (response.status === 204) {
        return null;
    }

    const contentType = response.headers.get('content-type') || '';
    if (contentType.includes('application/json')) {
        return response.json().catch(() => null);
    }

    const text = await response.text();
    return text.length > 0 ? text : null;
}

function buildUrl(endpoint) {
    if (/^https?:\/\//i.test(endpoint)) {
        return endpoint;
    }

    return `${API_CONFIG.BASE_URL}${endpoint}`;
}

function createStrongPassword(length = 18) {
    const alphabet = 'ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789!@$?_-';
    const randomBytes = new Uint8Array(length);

    if (window.crypto?.getRandomValues) {
        window.crypto.getRandomValues(randomBytes);
    } else {
        for (let i = 0; i < randomBytes.length; i += 1) {
            randomBytes[i] = Math.floor(Math.random() * 256);
        }
    }

    return Array.from(randomBytes, byte => alphabet[byte % alphabet.length]).join('');
}

let refreshPromise = null;

const API_CONFIG = {
    BASE_URL: resolveApiBaseUrl(),
    ENDPOINTS: {
        LOGIN: '/api/auth/login',
        FORGOT_PASSWORD: '/api/auth/forgot-password',
        RESET_PASSWORD: '/api/auth/reset-password',
        REFRESH: '/api/auth/refresh-token',
        LOGOUT: '/api/auth/logout',
        STOCKS: '/api/Stok',
        CUSTOMERS: '/api/Musteri',
        UNITS: '/api/Birim',
        ADMIN_ACTIVITIES: '/api/Admin/aktiviteler',
        ADMIN_ERRORS: '/api/Admin/sistem-hatalari',
        ADMIN_FILE_LOGS: '/api/Admin/dosya-loglari',
        ADMIN_CARITYPES: '/api/CariTip',
        ADMIN_MUSTERI_USERS: id => `/api/Admin/musteri/${id}/kullanicilar`,
        ADMIN_ADD_USER: id => `/api/Admin/musteri/${id}/kullanici`,
        ADMIN_DELETE_USER: id => `/api/Admin/kullanici/${id}`,
        ADMIN_SYSTEM_ADMINS: '/api/Admin/yoneticiler',
        ADMIN_ADD_SYSTEM_ADMIN: '/api/Admin/yonetici',
        ADMIN_UPDATE_SYSTEM_ADMIN: id => `/api/Admin/yonetici/${id}`,
        CARIS: '/api/CariKart'
    }
};

const apiClient = {
    getAccessToken() {
        return localStorage.getItem('atlas_token');
    },

    setAccessToken(token) {
        if (token) {
            localStorage.setItem('atlas_token', token);
            return;
        }

        localStorage.removeItem('atlas_token');
    },

    clearSession() {
        localStorage.removeItem('atlas_token');
        localStorage.removeItem('atlas_refresh');
    },

    redirectToLogin(expired = false) {
        if (window.location.pathname.includes('login.html')) {
            return;
        }

        window.location.href = expired ? 'login.html?expired=true' : 'login.html';
    },

    isAuthEndpoint(endpoint) {
        const normalized = endpoint.toLowerCase();
        return normalized.includes('/api/auth/login')
            || normalized.includes('/api/auth/refresh-token')
            || normalized.includes('/api/auth/logout');
    },

    async request(endpoint, options = {}, retryOnUnauthorized = true) {
        const headers = new Headers(options.headers || {});
        const hasBody = options.body !== undefined && options.body !== null;

        if (hasBody && !(options.body instanceof FormData) && !headers.has('Content-Type')) {
            headers.set('Content-Type', 'application/json');
        }

        const accessToken = this.getAccessToken();
        if (accessToken && !headers.has('Authorization')) {
            headers.set('Authorization', `Bearer ${accessToken}`);
        }

        const response = await fetch(buildUrl(endpoint), {
            ...options,
            headers,
            credentials: 'include'
        });

        if (response.status === 401 && retryOnUnauthorized && !this.isAuthEndpoint(endpoint)) {
            try {
                await this.refreshAccessToken();
                return this.request(endpoint, options, false);
            } catch (refreshError) {
                this.clearSession();
                this.redirectToLogin(true);
                throw refreshError;
            }
        }

        const payload = await readResponseBody(response);
        if (!response.ok) {
            const message = typeof payload === 'string'
                ? payload
                : payload?.hata || payload?.mesaj || 'Bir hata olustu.';

            const error = new Error(message);
            if (payload && typeof payload === 'object') {
                Object.assign(error, payload);
            } else {
                error.payload = payload;
            }

            error.status = response.status;
            throw error;
        }

        return payload;
    },

    async refreshAccessToken() {
        if (refreshPromise) {
            return refreshPromise;
        }

        refreshPromise = (async () => {
            const response = await fetch(buildUrl(API_CONFIG.ENDPOINTS.REFRESH), {
                method: 'POST',
                credentials: 'include'
            });

            const payload = await readResponseBody(response);
            if (!response.ok || !payload?.accessToken) {
                throw new Error(
                    typeof payload === 'string'
                        ? payload
                        : payload?.hata || 'Oturum yenilenemedi.'
                );
            }

            this.setAccessToken(payload.accessToken);
            return payload.accessToken;
        })().finally(() => {
            refreshPromise = null;
        });

        return refreshPromise;
    },

    async login(email, password) {
        const data = await this.request(API_CONFIG.ENDPOINTS.LOGIN, {
            method: 'POST',
            body: JSON.stringify({ ePosta: email, sifre: password })
        }, false);

        if (data?.accessToken) {
            this.setAccessToken(data.accessToken);
        }

        return data;
    },

    async forgotPassword(email) {
        return this.request(API_CONFIG.ENDPOINTS.FORGOT_PASSWORD, {
            method: 'POST',
            body: JSON.stringify({ ePosta: email })
        }, false);
    },

    async resetPassword(token, newPassword, confirmPassword) {
        return this.request(API_CONFIG.ENDPOINTS.RESET_PASSWORD, {
            method: 'POST',
            body: JSON.stringify({
                token,
                yeniSifre: newPassword,
                yeniSifreTekrar: confirmPassword
            })
        }, false);
    },

    async logout() {
        try {
            await this.request(API_CONFIG.ENDPOINTS.LOGOUT, { method: 'POST' }, false);
        } catch (error) {
            // Ignore logout response failures and clear the local session anyway.
        }

        this.clearSession();
        window.location.href = 'login.html';
    },

    async getStocks(page = 1, pageSize = 20) {
        const query = new URLSearchParams({ sayfa: page, sayfaBoyutu: pageSize });
        return this.request(`${API_CONFIG.ENDPOINTS.STOCKS}?${query}`);
    },

    async createStock(stockData) {
        return this.request(API_CONFIG.ENDPOINTS.STOCKS, {
            method: 'POST',
            body: JSON.stringify(stockData)
        });
    },

    async updateStock(id, stockData) {
        return this.request(`${API_CONFIG.ENDPOINTS.STOCKS}/${id}`, {
            method: 'PUT',
            body: JSON.stringify(stockData)
        });
    },

    async deleteStock(id) {
        return this.request(`${API_CONFIG.ENDPOINTS.STOCKS}/${id}`, {
            method: 'DELETE'
        });
    },

    async getCariCards(page = 1, pageSize = 20, search = '') {
        const query = new URLSearchParams({ sayfa: page, sayfaBoyutu: pageSize });
        if (search) {
            query.append('arama', search);
        }

        return this.request(`${API_CONFIG.ENDPOINTS.CARIS}?${query}`);
    },

    async createCariCard(data) {
        return this.request(API_CONFIG.ENDPOINTS.CARIS, {
            method: 'POST',
            body: JSON.stringify(data)
        });
    },

    async updateCariCard(id, data) {
        return this.request(`${API_CONFIG.ENDPOINTS.CARIS}/${id}`, {
            method: 'PUT',
            body: JSON.stringify(data)
        });
    },

    async deleteCariCard(id) {
        return this.request(`${API_CONFIG.ENDPOINTS.CARIS}/${id}`, {
            method: 'DELETE'
        });
    },

    async getMusteriler() {
        return this.request(API_CONFIG.ENDPOINTS.CUSTOMERS);
    },

    async getUnits() {
        return this.request(API_CONFIG.ENDPOINTS.UNITS);
    },

    async getCariTypes() {
        return this.request(API_CONFIG.ENDPOINTS.ADMIN_CARITYPES);
    },

    parseJwt(token) {
        try {
            const base64Url = token.split('.')[1];
            const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
            const jsonPayload = decodeURIComponent(atob(base64).split('').map(character => (
                `%${`00${character.charCodeAt(0).toString(16)}`.slice(-2)}`
            )).join(''));

            return JSON.parse(jsonPayload);
        } catch (error) {
            return null;
        }
    },

    getCurrentUser() {
        const token = this.getAccessToken();
        if (!token) {
            return null;
        }

        const payload = this.parseJwt(token);
        if (!payload) {
            return null;
        }

        const email = payload[JWT_CLAIMS.email] || payload.email || payload[JWT_CLAIMS.name] || '';
        const displayName = payload[JWT_CLAIMS.name] || email;

        return {
            token,
            payload,
            email,
            displayName,
            role: payload[JWT_CLAIMS.role] || payload.role || 'User',
            userId: payload[JWT_CLAIMS.userId] || payload.userId || '',
            musteriId: payload.MusteriId || null
        };
    },

    escapeHtml(value) {
        return String(value ?? '')
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#39;');
    },

    safeText(value, fallback = '-') {
        if (value === null || value === undefined || value === '') {
            return fallback;
        }

        return this.escapeHtml(value);
    },

    generateStrongPassword(length = 18) {
        return createStrongPassword(length);
    }
};
