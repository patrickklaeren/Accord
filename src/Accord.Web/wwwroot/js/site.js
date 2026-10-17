document.addEventListener('alpine:init', () => {
    const matches = (search, text) => text.toLowerCase().includes(search.trim().toLowerCase());

    Alpine.data('chatDemo', () => ({
        enhanced: false,
        paused: false,
        inView: true,
        typing: false,
        resetting: false,
        messages: [],
        exchange: 0,
        nextId: 0,
        timer: null,
        nextAction: null,
        remaining: 0,
        startedAt: 0,
        script: [
            {
                command: '/tag search async',
                title: 'async / await',
                text: 'Use await to wait for a task without blocking the calling thread.'
            },
            {
                command: '/remind me 30m Check the build',
                title: 'Reminder set',
                text: 'You will be reminded in 30 minutes.'
            },
            {
                command: '!eval 1 + 2',
                title: 'C# REPL succeeded',
                text: 'Result: System.Int32',
                code: '3'
            },
            {
                command: '/godbolt',
                title: 'Godbolt Compilation Result',
                text: 'C# · Compiler Explorer',
                code: 'public int Add(int a, int b) => a + b;'
            },
            {
                command: '/shorten https://example.com/docs',
                title: 'URL shortened',
                text: 'https://go.example/a1b2'
            }
        ],
        init() {
            this.motionPreference = window.matchMedia('(prefers-reduced-motion: reduce)');
            this.motionListener = () => this.applyMotionPreference();
            this.visibilityListener = () => document.hidden ? this.suspend() : this.resume();
            this.motionPreference.addEventListener('change', this.motionListener);
            document.addEventListener('visibilitychange', this.visibilityListener);
            if ('IntersectionObserver' in window) {
                this.observer = new IntersectionObserver(([entry]) => {
                    this.inView = entry.isIntersecting;
                    this.inView ? this.resume() : this.suspend();
                });
                this.observer.observe(this.$root);
            }
            this.applyMotionPreference();
        },
        destroy() {
            this.suspend();
            this.observer?.disconnect();
            this.motionPreference.removeEventListener('change', this.motionListener);
            document.removeEventListener('visibilitychange', this.visibilityListener);
        },
        applyMotionPreference() {
            this.suspend();
            this.enhanced = !this.motionPreference.matches;
            this.messages = [];
            this.exchange = 0;
            this.typing = false;
            this.resetting = false;
            this.nextAction = null;
            this.$nextTick(() => { this.$refs.viewport.scrollTop = 0; });
            if (this.enhanced) this.schedule(600, () => this.showCommand());
        },
        schedule(delay, action) {
            this.remaining = delay;
            this.nextAction = action;
            this.resume();
        },
        resume() {
            if (this.timer !== null || !this.enhanced || this.paused || !this.inView || document.hidden || !this.nextAction) return;
            this.startedAt = performance.now();
            this.timer = setTimeout(() => {
                this.timer = null;
                const action = this.nextAction;
                this.nextAction = null;
                action();
            }, this.remaining);
        },
        suspend() {
            if (this.timer === null) return;
            clearTimeout(this.timer);
            this.timer = null;
            this.remaining = Math.max(0, this.remaining - (performance.now() - this.startedAt));
        },
        toggle() {
            this.paused = !this.paused;
            this.paused ? this.suspend() : this.resume();
        },
        scrollToLatest() {
            this.$nextTick(() => {
                const viewport = this.$refs.viewport;
                viewport.scrollTo({ top: viewport.scrollHeight, behavior: 'smooth' });
            });
        },
        showCommand() {
            this.messages.push({ id: this.nextId++, bot: false, text: '', title: '', code: '' });
            this.scrollToLatest();
            this.typeCommand(0);
        },
        typeCommand(position) {
            const command = this.script[this.exchange].command;
            this.messages[this.messages.length - 1].text = command.slice(0, position);
            if (position < command.length) {
                this.schedule(45, () => this.typeCommand(position + 1));
            } else {
                this.typing = true;
                this.scrollToLatest();
                this.schedule(1100, () => this.showReply());
            }
        },
        showReply() {
            const reply = this.script[this.exchange];
            this.typing = false;
            this.messages.push({ id: this.nextId++, bot: true, title: reply.title, text: reply.text, code: reply.code ?? '' });
            this.scrollToLatest();
            this.schedule(3600, () => {
                this.exchange += 1;
                if (this.exchange < this.script.length) this.showCommand();
                else this.restartConversation();
            });
        },
        restartConversation() {
            this.resetting = true;
            this.schedule(350, () => {
                this.messages = [];
                this.exchange = 0;
                this.resetting = false;
                this.$refs.viewport.scrollTop = 0;
                this.schedule(500, () => this.showCommand());
            });
        }
    }));

    Alpine.data('tagBrowser', () => ({
        name: '',
        edit(link) {
            this.name = link.dataset.name;
            this.$refs.editForm.action = link.href;
            this.$refs.content.value = link.dataset.content;
            this.$refs.editDialog.showModal();
        },
        remove(link) {
            this.name = link.dataset.name;
            this.$refs.deleteForm.action = link.href;
            this.$refs.deleteDialog.showModal();
        }
    }));

    Alpine.data('commandBrowser', () => ({
        search: '',
        matches(text) { return matches(this.search, text); },
        groupMatches(group) {
            return Array.from(group.querySelectorAll('[data-command]')).some(command => this.matches(command.dataset.search));
        },
        get noMatches() {
            return !Array.from(this.$root.querySelectorAll('[data-command]')).some(command => this.matches(command.dataset.search));
        }
    }));

    Alpine.data('settingForm', () => ({
        busy: false,
        previous: false,
        init() { this.$nextTick(() => { this.previous = this.$refs.value?.checked ?? false; }); },
        sync(change) {
            const form = this.$root;
            if (form.elements.ChannelId?.value !== change.channel || form.elements.Flag?.value !== change.flag) return;
            if (this.$refs.value) this.$refs.value.checked = change.enabled;
            this.previous = change.enabled;
        },
        async save(event) {
            if (this.busy) return;
            const form = event.currentTarget;
            // Capture checkbox values before disabling controls. Discord IDs remain strings throughout.
            const data = new FormData(form);
            this.busy = true;
            try {
                const response = await fetch(form.action, {
                    method: 'POST',
                    body: data,
                    headers: { Accept: 'application/json' }
                });
                if (!response.headers.get('content-type')?.includes('application/json')) {
                    throw new Error(response.status === 403
                        ? 'You no longer have permission to make this change.'
                        : 'Your session may have expired. Reload the page and try again.');
                }
                const result = await response.json();
                if (!response.ok) throw new Error(result.message ?? 'The change could not be saved.');
                this.previous = this.$refs.value?.checked ?? false;
                this.$dispatch('notice', { message: result.message, error: false });
                if (data.has('ChannelId')) {
                    this.$dispatch('flags-updated', {
                        channel: data.get('ChannelId'), flag: data.get('Flag'), enabled: data.get('Enabled') === 'true'
                    });
                }
            } catch (error) {
                if (this.$refs.value) this.$refs.value.checked = this.previous;
                this.$dispatch('notice', { message: error.message ?? 'The change could not be saved. Try again.', error: true });
            } finally {
                this.busy = false;
            }
        }
    }));

    Alpine.data('flagSummary', () => ({
        refresh(change) {
            // Update both representations only after the server has confirmed the write.
            this.$root.querySelectorAll('[data-flag]').forEach(section => {
                if (section.dataset.flag !== change.flag) return;
                section.querySelectorAll('[data-channel]').forEach(channel => {
                    if (channel.dataset.channel === change.channel) channel.hidden = !change.enabled;
                });
                section.querySelector('[data-empty]').hidden = Array.from(section.querySelectorAll('[data-channel]'))
                    .some(channel => !channel.hidden);
            });
        }
    }));

    Alpine.data('notification', () => ({
        message: '',
        error: false,
        timer: null,
        show(notice) {
            clearTimeout(this.timer);
            this.message = notice.message;
            this.error = notice.error;
            this.timer = setTimeout(() => { this.message = ''; }, this.error ? 12000 : 5000);
        }
    }));
});
