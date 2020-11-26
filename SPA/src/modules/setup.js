function onWindowLoaded(callback) {
  if (document.readyState === 'complete') {
    callback();
  }
  else {
    window.addEventListener('load', callback);
  }
}

function serviceWorkerGuard() {
  if ('serviceWorker' in navigator) {
    return Promise.resolve(true);
  }
  else {
    return Promise.reject('ServiceWorker not supported on this device.');
  }
}

class Setup {

  registerServiceWorker() {
    return serviceWorkerGuard()
      .then(() => navigator.serviceWorker.register('/svc-worker.js', { scope: '/track/' }));
  }

  unregisterServiceWorkers() {
    return serviceWorkerGuard()
      .then(() => navigator.serviceWorker
        .getRegistrations()
        .then((regs) => regs.reduce(
          (promise, reg) => promise.then(() => reg.unregister()),
          Promise.resolve()))
      );
  }

  async setupServiceWorker() {
      await serviceWorkerGuard();
      onWindowLoaded(async () => {
          const reg = await this.registerServiceWorker();
          console.log(`${what}, registered with scope:`,  reg.scope);
      });
  }
  go() {
    return Promise.resolve()
      .then(() => this.setupServiceWorker())
  }
}

export default new Setup();