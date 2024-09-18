# Sharepoint List Job

This is a console application in **.NET Framework 4.8** to manage a SharePoint List, which is a collection of data that can be shared with team members and other site users. 

**SharePoint Lists** allow users to store, organise, and manage data in a tabular format, similar to a spreadsheet. They are highly customizable and can be used to create custom workflows, manage project data, and store document. 

This application was designed to interact with a SQL database and perform data synchronisation tasks, including inserting, updating, and deleting records from the SharePoint List as needed. 

## Key Technologies
The technologies used for this project included C#, .NET Framework 4.8, SQL Server 2019, and the SharePoint API.

## Usage
- Update the following settings in the *App.config*. 
![image](https://github.com/user-attachments/assets/f26383ad-9845-4b1f-b64c-626192876ca3)
- Update the DataAccessRepository with the list of columns to be updated in the Sharepoint List. Add, remove or update the list of property based on your requirements
![image](https://github.com/user-attachments/assets/c763e6e6-a83a-4f22-9b25-e96ccdde9b8c)
- Run the application and the Sharepoint List will be updated
![image](https://github.com/user-attachments/assets/723c159e-56ed-4983-84eb-a4d219673bd5)

To address the SharePoint API limitations, I implemented batch processing within the application. This approach reduced the number of API calls by grouping updates and only sending changes that were absolutely necessary. I also implemented rate-limiting logic to make it comply with the SharePoint API's request limits, which prevented timeouts and improved overall speed.

## License

**Eric Perez Villar**
