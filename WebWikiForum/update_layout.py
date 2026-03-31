import codecs

filepath = r'd:\Code\Webcuoiky\WebWikiForum\Views\Shared\_Layout.cshtml'
with codecs.open(filepath, 'r', 'utf-8') as f:
    text = f.read()

# 1. Logo
old_logo = '''<div class="text-primary p-1.5 rounded-lg bg-primary/10">

<span class="material-symbols-outlined text-[24px]">smart_display</span>

</div>

<span class="text-xl font-black tracking-tight text-slate-900 dark:text-white">VTWiki</span>'''

new_logo = '''<div class="w-10 h-10 flex items-center justify-center rounded-full bg-primary text-white shadow-sm ring-4 ring-primary/10">

<span class="material-symbols-outlined text-[20px]">smart_display</span>

</div>

<span class="text-xl font-black tracking-tight text-slate-900 dark:text-white ml-1"><span class="text-primary">VT</span>Wiki</span>'''

text = text.replace(old_logo, new_logo)

# 2. Home Active
old_home = '<a class="text-sm font-semibold text-slate-900 dark:text-white hover:text-primary transition-colors" href="/Home/Index">Home</a>'
new_home = '<a class="text-sm font-bold text-primary transition-colors" href="/Home/Index">Home</a>'
text = text.replace(old_home, new_home)

# 3. Nav links bold
text = text.replace('text-sm font-medium text-slate-600 dark:text-slate-400 hover:text-primary transition-colors py-2', 'text-sm font-bold text-slate-700 dark:text-slate-300 hover:text-primary transition-colors py-2')

# 4. Search bar wrapper
old_search_wrapper = '<div class="relative hidden sm:block w-full max-w-xs group z-50">'
new_search_wrapper = '<div class="relative hidden sm:block w-full max-w-md group z-50">'
text = text.replace(old_search_wrapper, new_search_wrapper)

# 5. Search bar input
old_search_input = 'border-slate-200 dark:border-slate-700 rounded-xl leading-5 bg-slate-50/50 dark:bg-slate-800/50 text-slate-900'
new_search_input = 'border-slate-200 dark:border-slate-700/50 rounded-full leading-5 bg-slate-50/80 dark:bg-slate-800/80 text-slate-900 font-medium tracking-wide'
text = text.replace(old_search_input, new_search_input)

# 6. Log In Button
old_login = '<a href="/Account/Login" class="hidden sm:flex items-center justify-center h-9 px-4 rounded-xl border border-slate-200 dark:border-slate-700 text-sm font-semibold text-slate-700 dark:text-slate-300 hover:bg-slate-50 dark:hover:bg-slate-800 transition-colors" style="display:inline-flex">'
new_login = '<a href="/Account/Login" class="hidden sm:flex items-center justify-center h-10 px-6 rounded-full border border-slate-200 dark:border-slate-700/50 text-sm font-bold text-slate-700 dark:text-slate-300 hover:bg-slate-50 dark:hover:bg-slate-800 transition-colors shadow-sm">'
text = text.replace(old_login, new_login)

# 7. Sign Up Button
old_signup = '<a href="/Account/CreateAccount" class="flex items-center justify-center h-9 px-4 rounded-xl bg-primary text-white text-sm font-bold shadow-md shadow-primary/20 hover:bg-primary-dark transition-colors" style="display:inline-flex">'
new_signup = '<a href="/Account/CreateAccount" class="flex items-center justify-center h-10 px-6 rounded-full bg-primary text-white text-sm font-bold shadow-sm shadow-primary/20 hover:bg-primary-dark transition-colors">'
text = text.replace(old_signup, new_signup)

with codecs.open(filepath, 'w', 'utf-8') as f:
    f.write(text)
print('Updated header successfully!')
