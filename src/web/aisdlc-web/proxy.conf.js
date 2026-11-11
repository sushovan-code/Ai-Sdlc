// Dev-server proxy: forwards /api to the API. Aspire injects the API address; the fallback is the API's launch profile.
const target = process.env['services__api__http__0'] || 'http://localhost:5073';

module.exports = {
  '/api': { target, secure: false, changeOrigin: true },
};
