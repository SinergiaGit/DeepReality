ibrary(jsonlite)

#STRUTTURA DEL SINGOLO PROFILO
#{"uid":"aq1oJl5k3IVq",
#  "events":
#  {"profile":
#  {"last_update":1532877464,
#          "clusters":{"35":18,"170":10,"4452":8,"11":12,"4698":11,"4460":18,"21":8,"7":19,"18":6,"560":11,"179":19,"6":14,"128":8,"4635":20,"283":5,"20":9,"24":20,"19":19,"175":9,"187":14,"31":5},
#          "geoloc":{"country":"IT","longitude":0,"latitude":0},
#          "sociodemos":{"21":13,"7":12,"26":7,"2":2,"22":8,"1":11,"23":10,"13":18,"25":7,"27":16,"6":10,"12":18,"20":17,"14":13,"24":4,"19":13,"10":7}},
#             "wam":{
#               "custom_segments":{"54516":1532850108,"54514":1532850108},
#               "techno":{"browser":"Other","device":"Mobile","isp":"Telecom Italia","os":"Android"},
#               "last_update":1532910979,"audiences":{"33937":1532850108,"28328":1532850108,"28417":1532850108,"28225":1532850108,"9783":1532850108,"13937":1532850108}}}}

###########################################################################
#data.frame 2 variabili
#uuid
#events
#
#
#
###lettura del file in input.
### Problema: il numero di connessioni è limitato
### soluzione: la function sucessivamente all'apertura di una connessione deve leggere e poi richiudere la connessione

library(sparklyr)
library(dplyr)
sc <- spark_connect(master = "local")

install.packages(devtools::install_github("mitre/sparklyr.nested"))
library(sparklyr.nested)

wb<-spark_read_json(sc = sc,name = "webo",path = "C:/Users/luigi/Desktop/working_directory/datiinput/datamining_20180729-000000_24.json")
meta<-spark_read_json(sc=sc, name = "meta", path = "C:/Users/luigi/Desktop/working_directory/datiinput/datamining_20180729-000000_24.json")
sdf_schema_viewer(meta)


sdf_select(wb,wb$events)
read_webo<-function(path){
  require(jsonlite)
  path<-(path)
  con<-file(path)
  out<-stream_in(con)
}


rd<-read_webo(path = "C:/Users/luigi/Desktop/working_directory/datiinput/datamining_20180729-000000_24.json")
con<-file("C:/Users/luigi/Desktop/working_directory/datiinput/datamining_20180729-000000_24.json")
out<-stream_in(con)

id<-rd$uid



a<-rd$events<-as.data.frame(rd$events)
id<-rd$uid
apf<-as.data.frame<-(a$profile)
apf$clusters
apf$geoloc
apf$sociodemos
apf$last_update
wam<-data.frame(a$wam)
df1<-data.frame(id,apf)

out$events$profile$last_update
##vanno convertite le date in formato YYY/MM/DD h/m/s
out$events$profile$last_update
out$events$wam$last_update


out$events$profile$last_update<-as.POSIXlt(out$events$profile$last_update, format="%Y%m%d-%H%M%S", origin="1970-01-01",tz="GMT")
out$events$profile$last_update<-as.Date(out$events$profile$last_update)


##this is a list 
out$events$wam$wcm$conversion

out$events$profile
### è posssibile utilizzare la function stream_out() per scrivere nel bucket????


###file meta
df<-read_json(path = "C:/Users/luigi/Desktop/working_directory/datiinput/datamining_20180729-000000_24.json.meta")
df<-data.frame(df)



##this is a list 
out$events$wam$wcm$conversion
##split della lista
library(tidyr)
ungroup(out$events$wam$wcm$conversion)

separate(out, out$events$wam$wcm$conversion, c("last_update","id"))



