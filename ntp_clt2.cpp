#include <iostream>
#include <string.h>
#include "ntp_unix.hpp"
#include <time.h>
using namespace std;
int main(){ 
     struct NTP_PACKET packet={0,};
     memset(&packet,0x1b,1);
     struct HOST_INFO** ifs = new HOST_INFO*[2];
     for(int i =0;i<1;i++){
        ifs[i]=new HOST_INFO();
        ifs[i]->port=PORT;
        if(i==0){
            ifs[i]->host=HOST;
        }else{
            ifs[i]->host="time.apple.com";
        }
     }
     //struct HOST_INFO* ifs = {{HOST,PORT,0},{"time.apple.com",123,0}};
    NTP_DATA dt = getNTP(&packet,ifs);
    cout<<dt.real_timestamp_sec<<endl;
    cout<<ctime((time_t*)&dt.real_timestamp_sec)<<endl;
    viewPacketInfo(dt.mPacket);
    cout<<"finish"<<endl;
return 0;
}
