import { useState } from 'react';
import { MessageSquare } from 'lucide-react';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { NotificationsList, PreferencesPanel } from '@/features/notifications';
import { ConversationList, MessageThread, NewConversationDialog } from '@/features/messaging';

export default function MessagesAndNotificationsPage() {
  const [tab, setTab] = useState('messages');
  const [activeConversationId, setActiveConversationId] = useState<string | null>(null);
  const [newConversationOpen, setNewConversationOpen] = useState(false);

  return (
    <div className="flex flex-1 flex-col">
      <div className="flex flex-col items-center gap-1 bg-page-banner px-6 py-5 text-center text-page-banner-foreground">
        <div className="flex items-center gap-3">
          <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-md bg-page-banner-foreground/15 text-page-banner-foreground">
            <MessageSquare className="h-5 w-5" />
          </span>
          <h1 className="text-xl font-semibold tracking-tight">Messages and Notifications</h1>
        </div>
        <p className="max-w-2xl text-sm text-page-banner-foreground/85">
          Notification center covering clinical, operational, administrative, and financial alerts, plus internal
          staff messaging.
        </p>
      </div>

      <div className="flex min-h-0 flex-1 flex-col p-4 lg:p-6">
        <Tabs value={tab} onValueChange={setTab} className="flex min-h-0 flex-1 flex-col">
          <TabsList>
            <TabsTrigger value="messages">Messages</TabsTrigger>
            <TabsTrigger value="notifications">Notifications</TabsTrigger>
            <TabsTrigger value="preferences">Preferences</TabsTrigger>
          </TabsList>

          <TabsContent value="messages" className="min-h-0 flex-1">
            <div className="flex h-[calc(100vh-15rem)] min-h-[24rem] overflow-hidden rounded-lg border border-border bg-background">
              <ConversationList
                activeConversationId={activeConversationId}
                onSelect={setActiveConversationId}
                onNewConversation={() => setNewConversationOpen(true)}
              />
              {activeConversationId ? (
                <MessageThread conversationId={activeConversationId} />
              ) : (
                <div className="flex flex-1 items-center justify-center p-8 text-center text-sm text-muted-foreground">
                  Select a conversation, or start a new one.
                </div>
              )}
            </div>
            <NewConversationDialog
              open={newConversationOpen}
              onOpenChange={setNewConversationOpen}
              onCreated={setActiveConversationId}
            />
          </TabsContent>

          <TabsContent value="notifications">
            <div className="mx-auto w-full max-w-2xl rounded-lg border border-border bg-background p-2">
              <NotificationsList />
            </div>
          </TabsContent>

          <TabsContent value="preferences">
            <div className="mx-auto w-full max-w-3xl rounded-lg border border-border bg-background p-4">
              <p className="mb-4 text-sm text-muted-foreground">
                Choose which channels you receive each kind of notification on. In-app notifications inside HMS are
                always free to enable; email and SMS depend on your hospital&apos;s configuration.
              </p>
              <PreferencesPanel />
            </div>
          </TabsContent>
        </Tabs>
      </div>
    </div>
  );
}
