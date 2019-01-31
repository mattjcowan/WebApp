const nuxtConfig = require('../shared/nuxt.config')

Object.assign(nuxtConfig.build, {
  publicPath: '/assets/'
})

// eslint-disable-next-line no-console
// console.log(nuxtConfig)

module.exports = Object.assign(nuxtConfig, {
  srcDir: __dirname,
  buildDir: './src-html/.nuxt/app',
  router: {
    base: '/app/'
  },
  generate: {
    dir: 'dist/wwwroot/app'
  }
})
