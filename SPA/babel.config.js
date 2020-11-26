module.exports = {
  presets: [
    [
      '@babel/preset-env', {
        targets: {
          browsers: ['> 1%', 'last 2 versions', 'not ie <= 8']
        }
      }
    ],
    '@babel/preset-typescript'
  ],
  plugins: [
    'transform-vue-jsx',
    '@babel/plugin-transform-runtime',
    '@babel/plugin-syntax-dynamic-import',
    '@babel/proposal-class-properties',
    '@babel/proposal-object-rest-spread'
  ]
};
