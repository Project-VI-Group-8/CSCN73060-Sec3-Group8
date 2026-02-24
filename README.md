# CSCN73060-Sec3-Group8
This is the GitHub repository for Project VI (CSCN73060) - a RESTful web server built with ASP.NET Core and PostgreSQL.

# Running the Application
## REQ
- Have Docker Engine running (Docker Desktop on Windows)
- Clone the Repo
- cd into root folder
- run ```docker compose up --build```
- This will compile and deploy the code to containers
- You can then access the frontend at localhost:3000/
- Backend api documentation is at     localhost:8000/swagger/

# Ports
## Frontend
- Razor    : port 3000
## Backend
- API      : port 8000
- SwaggerUI: localhost:8000/swagger/
## Database
- Postgres : port 5432
- PG Admin : port 5050

# Docker
## Build
- docker-compose up -build
## Run
- docker-compose -build
## Restart Service
- docker-compose restart api
## Stop
- docker-compose down
## RESET (CLEARS DATA/VOLUMES)
- docker-compose down -v
## Logs
- docker-compose logs
- docker-compose logs api


# Credentials
## PostgreSQL
- **User:** admin  
- **Password:** admin123  
- **Database:** postgres  

## pgAdmin
- **Email:** admin@admin.com  
- **Password:** admin123  

