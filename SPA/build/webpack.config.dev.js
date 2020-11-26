const buildConfig = require('./buildConfig');

let track = buildConfig({
  mode: 'development',
  entry: {
    'app': './src/main.js',
  },
  publicPath: '/track/',
  serviceWorker: './src/svc-worker.js'
});


module.exports = [track];
