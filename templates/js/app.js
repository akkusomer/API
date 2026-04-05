(function bootstrapAtlasShell() {
    function getTemplatePageName() {
        const pathSegments = window.location.pathname.split('/').filter(Boolean);
        const lastSegment = pathSegments[pathSegments.length - 1];

        if (!lastSegment || !lastSegment.includes('.')) {
            return 'index.html';
        }

        return lastSegment.toLowerCase();
    }

    function getCurrentUser() {
        try {
            return window.apiClient?.getCurrentUser?.() ?? null;
        } catch (error) {
            console.error('Current user okunamadi.', error);
            return null;
        }
    }

    function markActiveMenu() {
        const currentPath = getTemplatePageName();
        const menuItems = document.querySelectorAll('.menu-item[href]');

        menuItems.forEach(item => {
            const href = item.getAttribute('href');
            const isActive = href
                && href !== '#'
                && !href.startsWith('javascript:')
                && href.split('#')[0].toLowerCase() === currentPath;

            item.classList.toggle('active', Boolean(isActive));
        });
    }

    function routeGuard(pageName, currentUser) {
        const isLoginPage = pageName === 'login.html';
        const isResetPasswordPage = pageName === 'reset-password.html';
        const isPublicPage = isLoginPage || isResetPasswordPage;
        const isAdminPage = pageName === 'admin.html';

        if (!currentUser && !isPublicPage) {
            window.apiClient?.clearSession?.();
            window.apiClient?.redirectToLogin?.();
            return false;
        }

        if (isLoginPage && currentUser) {
            window.location.replace(currentUser.role === 'Admin' ? 'admin.html' : 'index.html');
            return false;
        }

        if (isAdminPage && currentUser?.role !== 'Admin') {
            window.location.replace('index.html');
            return false;
        }

        return true;
    }

    function buildAtlasBrand() {
        return {
            props: {
                context: {
                    type: String,
                    default: 'sidebar'
                },
                home: {
                    type: String,
                    default: 'index.html#dashboard'
                }
            },
            template: `
                <a :href="home" class="atlas-brand" :class="'atlas-brand--' + context" aria-label="Atlas Hal Programi">
                    <span class="atlas-brand__signal" aria-hidden="true">
                        <span v-for="n in 9" :key="n"></span>
                    </span>
                    <span class="atlas-brand__copy">
                        <span class="atlas-brand__title">ATLAS</span>
                        <span class="atlas-brand__subtitle">HAL PROGRAMI</span>
                    </span>
                </a>
            `
        };
    }

    function runLegacyShell() {
        document.addEventListener('DOMContentLoaded', () => {
            const pageName = getTemplatePageName();
            const currentUser = getCurrentUser();

            if (!routeGuard(pageName, currentUser)) {
                return;
            }

            markActiveMenu();
        });
    }

    if (!window.Vue) {
        runLegacyShell();
        return;
    }

    const { createApp } = window.Vue;

    document.addEventListener('DOMContentLoaded', () => {
        const root = document.querySelector('[data-vue-shell]');

        if (!root) {
            return;
        }

        const pageName = getTemplatePageName();

        const app = createApp({
            data() {
                return {
                    pageName,
                    currentUser: null
                };
            },
            computed: {
                userDisplayName() {
                    const source = (this.currentUser?.displayName || this.currentUser?.email || 'atlas').trim();
                    return source.includes('@') ? source.split('@')[0].toUpperCase() : source;
                },
                userRoleLabel() {
                    return this.currentUser?.role === 'Admin' ? 'Sistem Yoneticisi' : 'Musteri Kullanicisi';
                },
                userInitial() {
                    const source = (this.currentUser?.displayName || this.currentUser?.email || 'Atlas').trim();
                    return source.charAt(0).toUpperCase() || 'A';
                },
                adminDisplayName() {
                    return this.currentUser?.displayName || this.currentUser?.email || 'Sistem Yoneticisi';
                }
            },
            methods: {
                logout() {
                    window.apiClient?.logout?.();
                }
            },
            mounted() {
                const currentUser = getCurrentUser();

                if (!routeGuard(this.pageName, currentUser)) {
                    return;
                }

                this.currentUser = currentUser;
                this.$nextTick(() => {
                    document.body.classList.add('atlas-shell-ready');
                    markActiveMenu();
                });
            }
        });

        app.component('atlas-brand', buildAtlasBrand());
        app.mount(root);
    });
})();
