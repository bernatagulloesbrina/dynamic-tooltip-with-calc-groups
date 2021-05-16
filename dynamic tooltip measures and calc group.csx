// '2021-05-11 / B.Agullo / 
// '2021-05-16 / B.Agullo / added visibility measure, default tooltip
// Dynamic Tooltip Measures and Calc Group
// Creates the variables and calculation group for dynamic tooltips


// Instructions: 
// If the main column chart x axis is 'Visual Config'[Category], 
// there should be a column "Tooltip Type" in the same table which specifies 
// which tooltip chart or element should shown for each axis Category
// 
// Select the column containing the tooltip type and click run, 
// or save this script as quick action

// by default for each tooltip type 4 mesures will be created
// XXX Tooltip Value --> Values measure for the tooltip
// XXX Tooltip Title --> Title Measure for the tooltip 
// XXX Tooltip Background --> Background Measure for the tooltip 
// XXX Tooltip Visible --> boolean measure defining tooltip visibility
// XXX Tooltip Data Color --> Conditional formatting Measure for the tooltip 


string valuesMeasureName = "Value";
string titleMeasureName = "Title";
string backgroundMeasureName = "Background";
string visibilityMeasureName = "Visible";
string dataColorMeasureName = "Data Color"; 

string[] tooltipMeasures = {valuesMeasureName,titleMeasureName,backgroundMeasureName,visibilityMeasureName,dataColorMeasureName};
string tooltipMeasureSufix = "Tooltip";

string transparentColorMeasureName = "transparent";
 
string tooltipBackgroundColorMeasureName = "Tooltip Background Color";
string tooltipDefaultDataColorMeasureName = "Tooltip Default Data Color"; 
//string tooptipHighlightDataColorMeasureName = "Tooltip Highlight Data Color";


//default tooltip is only to use tooltip charts to swap over an existing chart
// this existing chart is the defaultTooltip 
bool createDetaultTooltypeMeasure =  true; 
string defaultTooltipTypeName = "Default"; 

//add the sufix of your calculation group here
string calcGroupName = "Tooltip Visibility";
string calcGroupColumnName = "Tooltip";
// -----


if (Selected.Columns.Count != 1) {
    Error("Select one and only one measure");
    return;
};

var tooltipTypeColumn = Selected.Column;
string tooltipTypeColumnReference = "'" + tooltipTypeColumn.Table.Name + "'[" + tooltipTypeColumn.Name + "]";

//update calcgroup name to include source table name 
calcGroupName = tooltipTypeColumn.Table.Name + " " + calcGroupName; 

//check if [baground] and [transparent] measure already exist 
// and creates them if they don't exist 
if(!Model.AllMeasures.Any(Measure => Measure.Name == tooltipBackgroundColorMeasureName)) {
    tooltipTypeColumn.Table.AddMeasure(tooltipBackgroundColorMeasureName,"\"#FFFFFF\"");
};

if(!Model.AllMeasures.Any(Measure => Measure.Name == transparentColorMeasureName)) {
    tooltipTypeColumn.Table.AddMeasure(transparentColorMeasureName,"\"#FFFFFF00\"");
};

if(!Model.AllMeasures.Any(Measure => Measure.Name == tooltipDefaultDataColorMeasureName)) {
    tooltipTypeColumn.Table.AddMeasure(tooltipDefaultDataColorMeasureName,"\"#CCCCCC\"");
};

//check to see if a table with this name already exists
//if it doesnt exist, create a calculation group with this name
if (!Model.Tables.Contains(calcGroupName)) {
  var cg = Model.AddCalculationGroup(calcGroupName);
  cg.Description = "Calculation group to control tooltip chart visibility";
};

//set variable for the calc group
Table calcGroup = Model.Tables[calcGroupName];

//if table already exists, make sure it is a Calculation Group type
if (calcGroup.SourceType.ToString() != "CalculationGroup") {
  Error("Table exists in Model but is not a Calculation Group. Rename the existing table or choose an alternative name for your Calculation Group.");
  return;
};

//by default the calc group has a column called Name. If this column is still called Name change this in line with specfied variable
if (calcGroup.Columns.Contains("Name")) {
  calcGroup.Columns["Name"].Name = calcGroupColumnName;
};
calcGroup.Columns[calcGroupColumnName].Description = "Apply each Item to respective tooltip chart, or all at once. Items are measure protected";

//query to evaluate to get the different tooltip types
string query = ""; 

if (createDetaultTooltypeMeasure) { 
    //adds default tooltip to the list
    query = 
        "EVALUATE " + 
        "    UNION( " + 
        "        VALUES(" + tooltipTypeColumnReference + ")," + 
        "       {\"" + defaultTooltipTypeName + "\"}" +
        "    )";
} else {
    //without the default tooltip
     query = 
        "EVALUATE " + 
        "    VALUES(" + tooltipTypeColumnReference + ")";
};

using (var reader = Model.Database.ExecuteReader(query))
{
    // Create a loop for every row in the resultset
    while(reader.Read())
    {   
        //get value from tooltip type column 
        string tooltipType = reader.GetValue(0).ToString();
        
        //display folder where all measures of the tooltip chart will go
        string measureDisplayFolder = tooltipType + " " + tooltipMeasureSufix + " Measures"; 
        
        //calculation group item that will apply visibility to the tooltip
        string calcItemName = tooltipType + " " + tooltipMeasureSufix;
        
        //base expression of calc item (placeholders replaced later) 
        string calcItemExpression = 
            "SWITCH (" + 
             "    TRUE ()," + 
             "    ISSELECTEDMEASURE ( [backgroundMeasureName] )," + 
             "        IF (" + 
             "            [visibilityMeasureName]," + 
             "            SELECTEDMEASURE ()," + 
             "            [" + transparentColorMeasureName +"]" + 
             "        )," + 
             "    ISSELECTEDMEASURE ( [valuesMeasureName], [titleMeasureName] )," + 
             "        IF (" + 
             "            [visibilityMeasureName]," + 
             "            SELECTEDMEASURE ()," + 
             "            BLANK ()" + 
             "        )" + 
             ")";
        
        //repeat process for each measure (background, value...) 
        foreach (string tooltipMeasure in tooltipMeasures) { 
        
            string measureName = tooltipType + " " + tooltipMeasureSufix + " " + tooltipMeasure ;
            //only variable intialization, actual value later 
            string measureExpression = ""; 
            string measureDescription = tooltipMeasure + " measure for " + tooltipType + " " + tooltipMeasureSufix;
            
            
            
            if (tooltipMeasure == backgroundMeasureName) {
                measureExpression= 
                    "VAR Result = [" + tooltipBackgroundColorMeasureName +"]" + 
                    "RETURN" + 
                    "    FORMAT ( Result, \"@\" )";
                     
                    calcItemExpression = calcItemExpression.Replace("backgroundMeasureName",measureName);
                            
            }else if(tooltipMeasure == dataColorMeasureName) {
                measureExpression = 
                    "VAR Result =[" + tooltipDefaultDataColorMeasureName + "] " + 
                     "RETURN" + 
                     "    FORMAT ( Result, \"@\" )";
                     
                
            }else if(tooltipMeasure == titleMeasureName) {
                measureExpression = "\"" + tooltipType + "\"";
               
                calcItemExpression = calcItemExpression.Replace("titleMeasureName",measureName);
                    
            }else if(tooltipMeasure == visibilityMeasureName) {
                
                //boolean measure which governs tooltip chart visibility
                if (tooltipType == defaultTooltipTypeName) {
                    //whenever a single value is selected hide chart
                    measureExpression = "NOT(HASONEVALUE ( " + tooltipTypeColumnReference + " ))";
                } else {
                    //boolean measure which governs tooltip chart visibility
                    measureExpression = "SELECTEDVALUE ( " + tooltipTypeColumnReference + " ) = \"" + tooltipType + "\"";
                };
                calcItemExpression = calcItemExpression.Replace("visibilityMeasureName",measureName);
                
            }else if(tooltipMeasure == valuesMeasureName) {
                measureExpression = 
                    "    /*replace 1 with your desired measure*/" +
                    "    1"  ;

                calcItemExpression = calcItemExpression.Replace("valuesMeasureName",measureName);
            };  
        
            //now we have everything to create the measure
            var newTooltipMeasure = 
                tooltipTypeColumn.Table.AddMeasure(measureName, measureExpression);
            newTooltipMeasure.FormatDax(); 
            newTooltipMeasure.DisplayFolder = measureDisplayFolder; 
            newTooltipMeasure.Description = measureDescription; 
        };
        
        //after creating the measures the calcItemExpression is now complete and we can create calcItem
        foreach(var cg in Model.CalculationGroups) {
            if (cg.Name == calcGroupName) {
                if (!cg.CalculationItems.Contains(calcItemName)) {
                    var newCalcItem = cg.AddCalculationItem(calcItemName, calcItemExpression);
                    newCalcItem.FormatDax();
                };
            };
        };
    };
};
   