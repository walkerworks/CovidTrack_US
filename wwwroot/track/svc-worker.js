importScripts("/track/precache-manifest.0c734ed431f2f473067be84063b2221c.js", "https://storage.googleapis.com/workbox-cdn/releases/3.6.3/workbox-sw.js");

/* eslint-disable no-undef */
function logForSvcWorker(message) {
  // eslint-disable-next-line prefer-template
  console.log('%cservice worker%c '+message, 'font-weight: bold; color: white; background-color: mediumslateblue; padding: 2px 6px; border-radius: 6px', 'font-weight: bold');
}

logForSvcWorker('svc-worker.js loaded 🎉');

importScripts(
  'svc-worker-env.js',         // defines self.__env
  'precache-manifest.js', // defines self.__precacheManifest
);

if (self.__env === 'development') {
  workbox.setConfig({debug: true});
}

workbox.skipWaiting();
workbox.clientsClaim();

// cache the build assets
// this has to be called before registerRoute, otherwise registerRoute would take precedence
workbox.precaching.precacheAndRoute([
  ...self.__precacheManifest,     // webpack build assets
  ...self.__precachManifest  // assets
], {
  cleanUrls: false
});

// we don't cache any API calls, at all
workbox.routing.registerRoute(
  /\/api\//,
  workbox.strategies.networkOnly()
);

workbox.routing.registerRoute(
  /precache-manifest\.js$/,
  workbox.strategies.networkOnly()
);

// catch-all for static files
workbox.routing.registerRoute(
  /\.(?:png|jpg|jpeg|svg|ico|css|html|js)$/,
  workbox.strategies.networkFirst()
);

