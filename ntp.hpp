#include <iostream>
#include <netdb.h>
#include <sys/types.h> 
#include <sys/socket.h>
#include <stdlib.h>
#include <string.h>
#include <unistd.h>
#include <arpa/inet.h>
#include <time.h>
#include <stdint.h> 
/*
https://github.com/imscs21/ntp
Made by imscs21
File Version 0.1α
Modify Date: 2017-1-17
*/
using namespace std;
#define HOST "time.windows.com"
#define PORT 123
#define NTP_CLT_SHOW_LOG 0
#define NTP_SOC_DEFAULT_TIMEOUT_SEC 10
#define TIME_PREFIX -2208988800l    
struct NTP_PACKET{
    unsigned char li :2;
unsigned char version :3;
    unsigned char mode :3;
    unsigned char stratum;
    signed char poll;
    signed char precision;
    int root_delay;
    int root_dispersion;
    int reference_identifier;
    unsigned long long ref_ts;
    unsigned long long origin_ts;
    unsigned long long recv_ts;
    unsigned long long trans_ts;
};
struct NTP_DATA{
  unsigned char data_received :1;
  unsigned char is_raw_timestamp :1;
  unsigned char is_raw_packet_ts :1;
  unsigned char data_state :3;
  unsigned char reserved_space :2;
  unsigned long long final_timestamp;
  unsigned long long real_timestamp_sec;
  struct NTP_PACKET* mPacket;
};
struct HOST_INFO{
    char* host;
    unsigned short port;
    unsigned char conn_mode;
};
unsigned long long ntohllWithOpt(unsigned long long* host_longlong,char isApplyData)
{
    if(isApplyData){
    int x = 1;
    if(*(char *)&x == 1)
       // return ((((unsigned long long)ntohl((unsigned long)host_longlong)) << 32) + ntohl(host_longlong >> 32));
      return (*host_longlong= (((unsigned long long)ntohl(*host_longlong>>32)))+(((unsigned long long)ntohl(*host_longlong))<<32));
   
    else
        return *host_longlong;
    }else{
        unsigned long long tmp = *host_longlong;
        return ntohllWithOpt(&tmp,1);
    }
 
}
void viewPacketInfo(struct NTP_PACKET* pck){
    if(pck!=NULL){
        const NTP_PACKET p=*pck;
        cout<<"===I-N-F-O==="<<endl;
        cout<<(int)p.li<<endl;
        cout<<(int)p.version<<endl;
        cout<<(int)p.mode<<endl;
        cout<<(int)p.stratum<<endl;
        cout<<(int)p.poll<<endl;
        cout<<(int)p.precision<<endl;
        cout<<p.root_delay<<endl;
        cout<<p.root_dispersion<<endl;
        cout<<p.reference_identifier<<endl;
        cout<<p.ref_ts<<endl;
        cout<<p.origin_ts<<endl;
        cout<<p.recv_ts<<endl;
        cout<<p.trans_ts<<endl;
        cout<<"===I-N-F-O==="<<endl;
    }
}
NTP_DATA getNTP(const NTP_PACKET* preSetting,HOST_INFO** hosts){//host_info must end with null structure!
    NTP_DATA rst = {0,};
    char foundNTP = 0;
    while(!foundNTP&& ((*hosts)!=NULL)){
        const HOST_INFO* hi = *hosts;
        int soc=-1;
        struct NTP_PACKET packet={0,};
        if(preSetting!=NULL){
            memcpy(&packet,preSetting,sizeof(packet));
        }else{
   // memset(&packet,0x1b,1);
            packet.li=2;
            packet.version=4;
            packet.mode=3;
            memset(&packet,0x1b,1);
        }
        if(NTP_CLT_SHOW_LOG){
            viewPacketInfo(&packet);
        }
        if((soc=socket(PF_INET,SOCK_DGRAM,0))!=-1){
            
            struct timeval tv;
            tv.tv_sec = NTP_SOC_DEFAULT_TIMEOUT_SEC;  /* 30 Secs Timeout */
            setsockopt(soc, SOL_SOCKET, SO_RCVTIMEO,(struct timeval *)&tv,sizeof(struct timeval));
            struct hostent* entry;
            if(!(entry=gethostbyname(hi->host))){
                rst.data_state=2;
                close(soc);
                hosts++;
                continue;
            }
            while(!rst.data_received&& (*entry->h_addr_list!=NULL)){
                struct sockaddr_in   mAddr;
                mAddr.sin_family     = AF_INET;
                mAddr.sin_port       = htons( hi->port);
               // mAddr.sin_addr= *entry->h_addr_list;
                memcpy((void*)(&mAddr.sin_addr),(void*)(*entry->h_addr_list),sizeof(mAddr.sin_addr));
                socklen_t addr_size  = sizeof( mAddr);
                entry->h_addr_list++;
                if(NTP_CLT_SHOW_LOG){
                    cout<<inet_ntoa(*(struct in_addr*)&(mAddr.sin_addr.s_addr))<<endl;
                    cout<<"setting ok"<<endl;
                }
                sendto( soc, (char*)&packet, sizeof(struct NTP_PACKET), 0, ( struct sockaddr*)&mAddr, sizeof( mAddr)); 
                if(NTP_CLT_SHOW_LOG){         
                    cout<<"send ok"<<endl;
                }
                recvfrom( soc, &packet, sizeof(struct NTP_PACKET), 0 , ( struct sockaddr *)&mAddr, &addr_size);
                if(NTP_CLT_SHOW_LOG){
                    cout<<"recv ok"<<endl;
                }
                close(soc);
                if(NTP_CLT_SHOW_LOG){
                    viewPacketInfo(&packet);
                }
                if(packet.recv_ts>0){
                    rst.data_state=0;
                    rst.data_received=foundNTP=1;
                }
                else{
                    rst.data_state=3;
                }
                unsigned long long tt = TIME_PREFIX+ ntohl(packet.recv_ts);//ntohllWithOpt(&packet.recv_ts,0)
                time_t ts=(time_t)tt;//ntohll(packet.recv_ts);
                rst.real_timestamp_sec=ts;
                if(NTP_CLT_SHOW_LOG){
                    cout<<"time stamp: "<<ts<<endl;
                    char* dt = ctime(&ts);
                    cout<<dt<<endl;
                }
            }
         //cout<<ctime(&ts)<<endl;

        }else{
            rst.data_state=1;
        }
       
        if(foundNTP){
            rst.data_received=1;
            NTP_PACKET* pck = (NTP_PACKET*)calloc(1,sizeof(struct NTP_PACKET));
            memcpy(pck,&packet,sizeof(struct NTP_PACKET));
            rst.mPacket=pck;
        }
        else{
             hosts++;
        }
    }
return rst;
}