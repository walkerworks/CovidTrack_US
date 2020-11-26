const MANIFEST_LINK_ID = 'manifest';
const DISPLAY_SEARCH_KEY = 'display';
const DISPLAY_STORAGE_KEY = 'pwa:manifest:display';
const INSTALL_STATE_STORAGE_KEY = 'pwa:install:complete';
const INSTALL_STATE_COMPLETE = 'complete';

const DISPLAY_MODES = Object.freeze([
  'fullscreen',
  'standalone',
  'minimal-ui',
  'browser'
]);

export const manifest = {
  displayMode: {
    allowedValues: Object.freeze([...DISPLAY_MODES, 'default']),
    isAllowed(value: string) {
      if (this.allowedValues.includes(value)) return true;
      console.warn(`${value} is not valid for manifest.display`);
      return false;
    },
    set(value: string) {
      if (!this.isAllowed(value)) return false;
      const link = document.getElementById(MANIFEST_LINK_ID) as HTMLLinkElement;
      const url = new URL(link.href);
      if (value === 'default') {
        url.searchParams.delete(DISPLAY_SEARCH_KEY);
      }
      else {
        url.searchParams.set(DISPLAY_SEARCH_KEY, value);
      }
      link.href = url.href;
      this.save(value);
      return true;
    },
    get() {
      const link = document.getElementById(MANIFEST_LINK_ID) as HTMLLinkElement;
      const url = new URL(link.href);
      return url.searchParams.get(DISPLAY_SEARCH_KEY) || 'default';
    },
    save(value: string) {
      if (!this.isAllowed(value)) return false;
      if (value === 'default') {
        localStorage.removeItem(DISPLAY_STORAGE_KEY);
      }
      else {
        localStorage.setItem(DISPLAY_STORAGE_KEY, value);
      }
      return true;
    },
    /** loads saved manifest display value from localStorage, if any */
    init() {
      const savedValue = localStorage.getItem(DISPLAY_STORAGE_KEY);
      if (savedValue) this.set(savedValue);
    }
  }
};

export const install = {
  get displayMode() {
    // for ios only
    if (window.navigator['standalone']) return 'standalone';

    let i = DISPLAY_MODES.length;
    while (i--) {
      const mode = DISPLAY_MODES[i];
      if (window.matchMedia(`(display-mode: ${mode})`).matches) {
        return mode;
      }
    }
    return undefined;
  },
  get complete(): boolean {
    const savedValue = localStorage.getItem(INSTALL_STATE_STORAGE_KEY);
    return savedValue === INSTALL_STATE_COMPLETE;
  },
  set complete(value: boolean) {
    if (value) {
      localStorage.setItem(INSTALL_STATE_STORAGE_KEY, INSTALL_STATE_COMPLETE);
    }
    else {
      localStorage.removeItem(INSTALL_STATE_STORAGE_KEY);
    }
  }
};