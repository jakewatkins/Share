# N8N with HTTPS

I need a docker-compose file that sets up the latest version of N8N with a reverse proxy so I can use HTTPS to access N8N.  The reason I need this configuration is external services that use OAuth require the redirect URL to use HTTPS.
This docker-compose file will serve as a starting point for other solutions I'll build.  As such it should be arrange so I can easily add new services.

## Clarifications
- there isn't a domain name, this is all development in my local lab.  The host names will change.  For example, right way we'll deploy this to fairladyz.local 
- self-signed certificates work
- Traefix sounds wonderful
- Start with standard ports, we'll change as needed when needed.
- lab-network is a good name for the docker network name.
- the usual N8N configurations settings will be fine, I can add whatever is missing 
- Setup 2 volumes for N8N: logs and data.  I'll modify the path as needed
- right now we just need N8N to use HTTPS so I can configure a slack trigger.
