SwissProt.xml.gSqlFileStreamWebApiDemo
=======================

# Overview
The basic idea is to serve large JSON or XML files directly as streams of binary data. If a typical Entity Framwork and serializer is used, the web server must hold both large objects and the serialized result in memory for as long as the request is active. For responses around 100 MB this will lead to performance problems, also for SQL servers. There are severs schemes to avoid this, the one I am looking into in this demo is my favorite.

Someone has to gzip and store the data in SQL, this approach is more a cache for large responses.
When the request comes, we get a handle to the file that SQL Server is using as its underlying storage. This allows us to stream the raw data directly to the client, and simply by stating the encoding and content-type the client renders the data correctly. Gzip is unversially understood by web clients. I tried the example with PowerQuery in 64 bit Excel 2013 and it works very well to consume data directly from the web server.

              +-----------------+
              |                 |
              |     Browser     |
              |                 |
              |  Unpacking gzip |
              +--------+--------+
                       | Content-Encoding: gzip
                       | Content-Type: application/xml
              +--------+--------+
              |                 |
              |    Web API      | Web API serving a binary stream
              |                 | directly from SQL Server using
              +--------+--------+ SqlFileStream
                       |
              +--------+--------+
              |                 |
              |    SQL Server   | SQL Server is storing the data on disk
              |                 |
              +--------+--------+
                       |
              +--------+--------+
              |                 |
              |    Filesystem   |
              |                 |
              +-----------------+

## In node.js
A simpler example that demonstrates the principle can be tested in node.js using a file directly.

```javascript      
var http = require('http');
var fs = require('fs');
http.createServer(function (req, res) {
    res.writeHead(200, {'Content-Type': 'application/xml',
                        'Content-Encoding':'gzip'});  
    fs.createReadStream('SwissProt.xml.gz').pipe(res);
}).listen(1337, '127.0.0.1');
```
              
# Getting the DB set up and testdata

Obviously you can use whatever huge XML or JSON file with Enterprise data that you happen to have, but if you don't have any data lying around then get the testdata from z available from http://www.cs.washington.edu/research/xmldatasets/www/repository.html
Direct download link: http://www.cs.washington.edu/research/xmldatasets/data/SwissProt/SwissProt.xml.gz

The gzip file is about 12% the size of the original XML file.

## Configuring SQL Server FILESTREAM
I think it is better to follow the instructions in "Appendix A: Configuring FILESTREAM on a SQL Server Instance" in
http://download.red-gate.com/ebooks/SQL/Art_of_SS_Filestream_Sebastian_and_Aelterman.pdf

There are some configurations that must be done both in SQL Server Configuration Manager and in SQL Server Management Studio. 


## The database
FILESTREAM fields can not be initialized from the user interface, but must be added by script.
I just dumped my create script...
 ```sql
USE [MyFastDB]
GO

/****** Object:  Table [dbo].[FASTTABLE]    Script Date: 29.12.2013 21:47:33 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

SET ANSI_PADDING ON
GO

CREATE TABLE [dbo].[FASTTABLE](
	[ID] [uniqueidentifier] ROWGUIDCOL  NOT NULL,
	[ZippedXML] [varbinary](max) FILESTREAM  NULL,
 CONSTRAINT [PK_FASTTABLE] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY] FILESTREAM_ON [MyFastDBfs],
UNIQUE NONCLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] FILESTREAM_ON [MyFastDBfs]

GO

SET ANSI_PADDING OFF
GO
 ```



## Insert testdata in the database
I prefer to use LINQPad, so I simply insert the .gz file into the manually created table with a carefully chosen GUID already inserted in a row.
 ```cs
var guid = new Guid("1277453e-6894-4c57-95b3-8498b316d43a");
FASTTABLEs.Where(r => r.ID == guid).First().ZippedXML =
			File.ReadAllBytes(@"C:\Users\beb\Downloads\SwissProt.xml.gz");
SubmitChanges();
 ```

# Running the application
The Visual Studio 2013 project should fetch its dependencies using Nuget and run without problems...
I could only parse this large files in Chrome and using Excel PowerQuery, but try it in your favorite browser that you think is capable of handling it...
