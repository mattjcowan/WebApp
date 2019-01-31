const nuxtConfig = require('../shared/nuxt.config')

Object.assign(nuxtConfig.build, {
  publicPath: '/assets/'
})

module.exports = Object.assign(nuxtConfig, {
  srcDir: __dirname,
  buildDir: './src-html/.nuxt/public',
  generate: {
    dir: 'dist/wwwroot'
  }
})
