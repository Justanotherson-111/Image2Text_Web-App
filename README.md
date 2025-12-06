This is a small, one-person project for the school. This is a web-app project, which main feature is to extract text from image using Tesseract Ocr. 
Programming Lang: C#, TypeScript. Using ASP.NET and React for backend and frontend of this Project.
This Project has been config to work on both window and linux OS using Docker.
I'm also using mkcert for ssl. For installation check this:https://github.com/FiloSottile/mkcert
I'm mainly using Ubuntu while developing this, haven't test if can deploy on windows but should be working by using Docker.

To test this web app:
  1. Simply pull the whole project.
  2. Go to docker-compose.yml/appsettings.json -> change {} to your database credentials.
  3. Make sure Node.js has download in your pc. For installation: https://nodejs.org/en/download (optional)
  4. If you have install Node.js then run "run-app.js" this will prompt cd command for this project. (This is it!!)
  5. If you haven't install Node.js you can open terminal.
  6. Then "cd /.../.../Project" then to run "docker compose up" to stop "docker compose down" (This is it!!)
  7. I have create a dummy account username: admin / password: Admin123! so you can use this to check the tesseract. Or you can simply create a new account!! (My Project currently have role:user/admin)

Conclusion: This project isn't some thing I poured my heart into, this is simply to improve my skill and show that this is the basic things I can do.
So if you have suggestions to improve this, I'll listen and learn, but don't expect much that i'll immediately improve this Project.(I just don't have time and resources to do so).
