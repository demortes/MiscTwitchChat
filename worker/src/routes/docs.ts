import { Hono } from 'hono';
import type { AppEnv } from '../env';
import { openapi } from '../openapi';

export const docs = new Hono<AppEnv>();

const SWAGGER_UI_VERSION = '5.17.14';

docs.get('/openapi.json', (c) => c.json(openapi));
docs.get('/swagger/v1/swagger.json', (c) => c.json(openapi));

docs.get('/swagger', (c) =>
  c.html(`<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="utf-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1.0" />
  <title>Swagger - Dem's Twitch API</title>
  <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/swagger-ui-dist@${SWAGGER_UI_VERSION}/swagger-ui.css" />
  <link rel="stylesheet" href="/css/custom.css" />
</head>
<body>
  <div id="swagger-ui"></div>
  <script src="https://cdn.jsdelivr.net/npm/swagger-ui-dist@${SWAGGER_UI_VERSION}/swagger-ui-bundle.js"></script>
  <script>
    window.onload = function () {
      window.ui = SwaggerUIBundle({ url: '/openapi.json', dom_id: '#swagger-ui' });
    };
  </script>
</body>
</html>`),
);
