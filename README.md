SqlFileStreamWebApiDemo
=======================

## Getting testdata

Obviously you can use whatever huge XML or JSON file with Enterprise data that you happen to have, but if you don't have any data lying around then get the testdata from SwissProt.xml.gz available from http://www.cs.washington.edu/research/xmldatasets/www/repository.html
Direct download link: http://www.cs.washington.edu/research/xmldatasets/data/SwissProt/SwissProt.xml.gz

The gzip file is about 12% the size of the original XML file.

## Configuring SQL Server FILESTREAM
I think it is better to follow the instructions here, but there are some manual configurations that must be done both in
SQL Server Configuration Manager and in SQL Server Management Studio.

http://download.red-gate.com/ebooks/SQL/Art_of_SS_Filestream_Sebastian_and_Aelterman.pdf

## Insert testdata in the database
I prefer to use LINQPad, so I simply insert the .gz file into the manually created table with a carefully chosen GUID already inserted in a row.
 ```cs
var guid = new Guid("1277453e-6894-4c57-95b3-8498b316d43a");
var row1 = FASTTABLEs.Where(r => r.ID == guid).First().ZippedXML =
			File.ReadAllBytes(@"C:\Users\beb\Downloads\SwissProt.xml.gz");
SubmitChanges();
 ```

## Running the application
The Visual Studio 2013 project should fetch its dependencies using Nuget and run without problems...
I could only parse this large files in Chrome and using Excel PowerQuery, but try it in your favorite browser that you think is capable of handling it...
