const webpack = require('webpack');
const CopyWebpackPlugin = require('copy-webpack-plugin');
const VueLoaderPlugin = require('vue-loader/lib/plugin');
const MiniCssExtractPlugin = require('mini-css-extract-plugin');
const WriteFilePlugin = require('write-file-webpack-plugin');
const CleanWebpackPlugin = require('clean-webpack-plugin');
const WorkboxPlugin = require('workbox-webpack-plugin');
const OptimizeCSSPlugin = require('optimize-css-assets-webpack-plugin');

const path = require('path');
const fs = require('fs');

const { CTUS_PASSPHRASE, HOST, HMR } = process.env;
if (!CTUS_PASSPHRASE) {
  throw new Error('Environment variable CTUS_PASSPHRASE must be defined to run this configuration');
}

function resolvePath(dir) {
  return path.join(__dirname, '..', dir);
}

const hot = HMR ? true : false;

module.exports = function(options) {
  if (!options.mode) throw new Error('mode is required');
  if (!options.publicPath) throw new Error('publicPath is required');
  if (!options.entry) throw new Error('entry is required');
  if (!options.outputPath) {
    options.outputPath = `../wwwroot${options.publicPath}`;
  }

  const config = {
    mode: options.mode,
    entry: options.entry,
    output: {
      path: resolvePath(options.outputPath),
      filename: '[name].[hash:7].js',
      publicPath: options.publicPath,
      globalObject: '(typeof self !== \'undefined\' ? self : this)'
    },
    resolve: {
      extensions: ['.js', '.ts', '.vue', '.json'],
      alias: {
        'vue$': 'vue/dist/vue.esm.js',
        '~': resolvePath('./src'),
      }
    },
    devServer: {
      hot: hot,
      open: false, // true opens a new browser window
      compress: true,
      contentBase: resolvePath(options.outputPath),
      host: HOST || 'loc.covid-track.us',
      port: (options.devServer || {}).port || 8080,
      allowedHosts: ['.covid-track.us'],
      disableHostCheck: true,
      https: {
        pfx: fs.readFileSync('../../loc.covid-track.us.pfx'),
        passphrase: CTUS_PASSPHRASE
      },
      stats: { // reduces verbosity of output
        modules: false
      }
    },
    module: {
      rules: [
        {
          test: /\.(js|ts)$/,
          use: 'babel-loader',
          include: [resolvePath('./src')],
        },
        {
          test: /\.vue$/,
          use: 'vue-loader',
        },
        {
          test: /\.s(c|a)ss$/,
          use: [
            'css-loader',
            'vue-style-loader',
            {
              loader: 'sass-loader',
              options: {
                implementation: require('sass'),
                sassOptions: {
                  indentedSyntax: true // optional
                },
              },
            },
          ],
        },
        {
          test: /\.css$/,
          use: [
            'style-loader',
            'css-loader',
          ],
        },
        {
          test: /\.(png|jpe?g|gif|svg)(\?.*)?$/,
          loader: 'url-loader',
          options: {
            limit: 10000,
            name: 'images/[name].[hash:7].[ext]'
          }
        },
        {
          test: /\.(mp4|webm|ogg|mp3|wav|flac|aac)(\?.*)?$/,
          loader: 'url-loader',
          options: {
            limit: 10000,
            name: 'media/[name].[hash:7].[ext]'
          }
        },
        {
          test: /\.(woff2?|eot|ttf|otf)(\?.*)?$/,
          loader: 'url-loader',
          options: {
            limit: 10000,
            name: 'fonts/[name].[hash:7].[ext]'
          }
        }
      ]
    },
    externals: function (context, request, callback) {
      if (/xlsx|canvg|pdfmake/.test(request)) {
        return callback(null, `commonjs ${  request}`);
      }
      callback();
    }
  };

  const plugins = [];
  plugins.push(new webpack.DefinePlugin({
    'process.env': JSON.stringify(options.mode),
  }));
  plugins.push(new webpack.HotModuleReplacementPlugin());
  plugins.push(new VueLoaderPlugin());
  plugins.push(new MiniCssExtractPlugin({
    filename: 'app.[hash:7].css'
  }));

  if (options.mode === 'production') {
    plugins.push(new OptimizeCSSPlugin({
      cssProcessorOptions: {
        safe: true,
        map: { inline: false }
      }
    }));
    plugins.push(new webpack.HashedModuleIdsPlugin());
  }

  plugins.push(new CopyWebpackPlugin([{
    from: resolvePath('./public'),
    to: resolvePath(options.outputPath),
    ignore: ['index.html'], // this gets transformed by HtmlWebpackPlugin above
    transform(content) {
      return content
        .toString()
        .replace('$ENV', options.mode);
    }
  }]));

  // write static assets to disk (normal dev behavior is in-memory)
  plugins.push(new WriteFilePlugin({}));

  // cleanup between builds and reloads
  plugins.push(new CleanWebpackPlugin({
    cleanAfterEveryBuildPatterns: ['!**/*', '*.hot-update.json', '*.hot-update.js', 'precache-manifest.*.js']
  }));

  if (options.serviceWorker) {
    plugins.push(new WorkboxPlugin.InjectManifest({
      swSrc: resolvePath(options.serviceWorker)
    })
    );
  }

  config.plugins = plugins;

  return config;
};

